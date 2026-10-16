# env-2 prototype: an ascii map of a candidate reshaped height (ponds, stream,
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

def river(x, z):
    return VN(x, z, 140, 71) * 0.8 + VN(x, z, 35, 74) * 0.2

# Meters from the river line (where river() crosses 0.5): the value's offset
# divided by its slope, so flat stretches of noise don't widen the stream.
def river_distance(x, z):
    r = river(x, z)
    gx = (river(x + 1, z) - river(x - 1, z)) / 2
    gz = (river(x, z + 1) - river(x, z - 1)) / 2
    return abs(r - 0.5) / max(math.hypot(gx, gz), 0.002)

def land(x, z):
    hills = VN(x, z, 48, 1) * 2.0 + VN(x, z, 8, 2) * 0.4 - 0.6
    # The water plane is flat, so the stream runs in a broad valley: hills sink
    # toward a floor just above water within ~26 m of the river line, then a
    # channel ~3 m wide is cut to a fixed bed below water.
    meters = river_distance(x, z)
    valley = 1 - ss(4, 26, meters)
    bed = 1 - ss(1.2, 3.5, meters)
    h = hills * (1 - 0.85 * valley) - 0.55 * 0.85 * valley
    h = h * (1 - bed) + (WL - 0.35) * bed
    # Ponds: round hollows filled to a fixed depth.
    pond = ss(0.86, 0.95, VN(x, z, 36, 70))
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
