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

def land(x, z):
    hills = VN(x, z, 48, 1) * 2.0 + VN(x, z, 8, 2) * 0.4 - 0.6
    pond = ss(0.78, 0.92, VN(x, z, 36, 70)) * 2.4
    # The water plane is flat (World.WaterLevel), so a stream must run in a valley:
    # TODO lower the hills broadly along the river line, then carve the bed to a
    # fixed depth below water instead of subtracting.
    w = abs(VN(x, z, 110, 71) - 0.5)
    stream = (1 - ss(0.006, 0.02, w)) * 1.6 * ss(0.2, 0.5, VN(x, z, 200, 73) + 0.3)
    keep = ss(18, 32, math.hypot(x, z))
    return hills * (0.4 + 0.6 * keep) + 0.1 * (1 - keep) - max(pond, stream) * keep

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
