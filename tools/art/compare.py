"""Stack turntable renders into one labelled comparison sheet (art-rnd-1).

    python3 tools/art/compare.py out.png "label one" a.png "label two" b.png ...

Each render is cropped to its middle band (the figures) and stacked with its label.
"""
import sys

from PIL import Image, ImageDraw, ImageFont

out, pairs = sys.argv[1], sys.argv[2:]
rows = []
for label, path in zip(pairs[::2], pairs[1::2]):
    im = Image.open(path).convert("RGB")
    w, h = im.size
    rows.append((label, im.crop((0, int(h * 0.22), w, int(h * 0.74)))))
font = None
for name in ("DejaVuSans-Bold.ttf", "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"):
    try:
        font = ImageFont.truetype(name, 34)
        break
    except OSError:
        pass
font = font or ImageFont.load_default()
W = rows[0][1].width
H = sum(r.height for _, r in rows)
sheet = Image.new("RGB", (W, H), (30, 30, 30))
y = 0
for label, r in rows:
    sheet.paste(r, (0, y))
    d = ImageDraw.Draw(sheet)
    d.rectangle((0, y, d.textlength(label, font=font) + 28, y + 52), fill=(20, 20, 20))
    d.text((14, y + 8), label, fill=(240, 236, 220), font=font)
    y += r.height
sheet.save(out)
print("saved", out)
