# env-2: an ascii map of the reshaped height in shared/Land.cs (keep the two in step) (ponds, stream,
# gentler hills) built on a Python copy of shared/World.cs noise. Scratch tooling
# for the Environment lane; nothing in the game reads it.
# Run: python3 -I tools/env-preview/land.py
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from world import VN

def ss(a, b, x):
    t = min(max((x - a) / (b - a), 0), 1)
    return t * t * (3 - 2 * t)

WL = -1.0  # World.WaterLevel

# Streams run roughly north to south, one every RiverSpacing meters, wandering
# side to side by a warp gentle enough (slope under 1) that a stream never
# doubles back into a loop.
RiverSpacing = 300.0

def river(x, z):
    warp = (VN(x, z, 120, 71) - 0.5) * 70 + (VN(x, z, 40, 74) - 0.5) * 18 \
        + (VN(x + 2741, z - 5167, 22, 77) - 0.5) * 18 + (VN(x - 6211, z + 3343, 11, 78) - 0.5) * 3
    return x + 66 + warp  # the first stream passes about 66 m west of spawn

def river_distance(x, z):
    u = river(x, z)
    off = abs(u - RiverSpacing * round(u / RiverSpacing))
    gx = (river(x + 1, z) - river(x - 1, z)) / 2
    gz = (river(x, z + 1) - river(x, z - 1)) / 2
    return off / max(math.hypot(gx, gz), 0.2)

def land(x, z):
    hills = VN(x, z, 48, 1) * 2.0 + VN(x, z, 8, 2) * 0.4 - 0.6
    # The water plane is flat, so the stream runs in a broad valley: hills sink
    # toward a floor just above water within ~26 m of the river line, then a
    # channel ~3 m wide is cut to a fixed bed below water.
    meters = river_distance(x, z)
    valley = 1 - ss(4, 26, meters)
    width = 0.85 + 0.5 * VN(x + 3907, z - 1613, 30, 76)
    bed = 1 - ss(1.0 * width, 6.0 * width, meters)
    h = hills * (1 - 0.85 * valley) - 0.55 * 0.85 * valley
    h = h * (1 - bed) + (WL - 0.22) * bed  # wadeable, see Land.cs
    # Ponds: round hollows filled to a fixed depth, in a wider dip so banks slope
    # gently. Offsets keep them off the
    # noise lattice (and off the axes, where the hash is weak).
    field = VN(x + 5113, z - 2971, 26, 70) * 0.6 + VN(x - 3307, z + 7741, 16.4, 75) * 0.4
    h += (min(h, -0.6) - h) * ss(0.36, 0.74, field)
    pond = ss(0.68, 0.82, field)
    h += (min(h, WL - 0.4) - h) * pond
    keep = ss(18, 32, math.hypot(x, z))  # spawn stays a dry, gentle rise
    return h * keep + (0.1 + hills * 0.3) * (1 - keep)

water = n = 0
for z in range(-120, 121, 4):
    row = ""
    for x in range(-150, 151, 3):
        h = land(x, z); n += 1
        if h < -1: water += 1; row += "~"
        elif x == 0 and z == 0: row += "S"
        else: row += " .:-=+*#%@"[min(9, max(0, int((h + 1) / 0.35)))]
    print(row)
print("water fraction", round(water / n, 3))
