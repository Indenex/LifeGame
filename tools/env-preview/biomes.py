# world-1: draws the biome map of shared/Biomes.cs (keep the two in step), as an ascii
# summary and a PNG. Scratch tooling; nothing in the game reads it.
# Run: python3 -I tools/env-preview/biomes.py [out.png] [half-size-in-meters]
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from world import VN

SCALE, THRESHOLD, BLEND, KEEP, FADE = 400.0, 0.62, 0.12, 150.0, 100.0

def ss(a, b, v):
    t = min(max((v - a) / (b - a), 0), 1)
    return t * t * (3 - 2 * t)

def ragged(x, z, salt): return (VN(x, z, 45, salt) - 0.5) * 0.12

def strength(kind, x, z):
    raw = VN(x + 1301, z - 877, SCALE, 200) + ragged(x, z, 202) if kind == "marsh" \
        else VN(x - 2203, z + 1559, SCALE, 201) + ragged(x, z, 203)
    away = ss(KEEP, KEEP + FADE, math.hypot(x, z))
    return ss(THRESHOLD, THRESHOLD + BLEND, raw - (1 - away))

def biome(x, z):
    m, h = strength("marsh", x, z), strength("highland", x, z)
    if h > 0.5 and h >= m: return "highland"
    if m > 0.5: return "marsh"
    return "forest"

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else None
    half = int(sys.argv[2]) if len(sys.argv) > 2 else 1500
    step = max(half // 100, 1)
    counts = {"forest": 0, "marsh": 0, "highland": 0}
    for z in range(-half, half + 1, step):
        row = ""
        for x in range(-half, half + 1, step):
            b = biome(x, z); counts[b] += 1
            row += "S" if x == 0 and z == 0 else {"forest": ".", "marsh": "~", "highland": "^"}[b]
        print(row[::2])
    n = sum(counts.values())
    print({k: round(v / n, 3) for k, v in counts.items()})
    if out:
        from PIL import Image, ImageDraw
        colors = {"forest": (86, 128, 72), "marsh": (96, 120, 128), "highland": (150, 132, 150)}
        px = 480
        img = Image.new("RGB", (px, px))
        for j in range(px):
            for i in range(px):
                x = round((i / px - 0.5) * 2 * half); z = round((j / px - 0.5) * 2 * half)
                img.putpixel((i, j), colors[biome(x, z)])
        d = ImageDraw.Draw(img)
        c = px // 2
        d.ellipse((c - 3, c - 3, c + 3, c + 3), fill=(255, 255, 255))
        d.text((8, 8), f"biomes, {2 * half} m square, spawn = white dot", fill=(255, 255, 255))
        img.save(out)
