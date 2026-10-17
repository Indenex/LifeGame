#!/usr/bin/env python3
"""Packs the Golden Skull foliage sprites the ground cover uses into one atlas
(env-13). Reads docs/art-packs/golden-skull/foliage (Justin's licensed pack; see its
README) and writes client/assets/environment/foliage_atlas.png, one 256 px cell per
sprite, each scaled to fit and standing on the bottom of its cell. Prints the C#
table (cell, width / height) PaintedGround.Cards uses. Run from the repo root:

    python3 tools/env-assets/build_foliage_atlas.py
"""
from pathlib import Path
from PIL import Image

PACK = Path("docs/art-packs/golden-skull/foliage")
OUT = Path("client/assets/environment/foliage_atlas.png")
CELL, COLS = 256, 8
# Order is the cell index; never reorder (append new sprites at the end).
SPRITES = [
    "Forest_small_grass_01", "Forest_small_grass_02", "Forest_small_grass_03", "Forest_small_grass_04",
    "Forest_small_grass_05", "Forest_small_grass_06", "Forest_small_grass_07",
    "Forest_small_flower_01", "Forest_small_flower_02", "Forest_small_flower_03",
    "Forest_small_clover_01", "Forest_small_clover_02", "Forest_small_clover_03",
    "Forest_small_fern_01", "Forest_small_fern_02",
    "Forest_small_mushroom_01", "Forest_small_mushroom_02", "Forest_small_mushroom_03", "Forest_small_mushroom_04",
]

rows = (len(SPRITES) + COLS - 1) // COLS
atlas = Image.new("RGBA", (CELL * COLS, CELL * 4), (0, 0, 0, 0))
table = []
for i, name in enumerate(SPRITES):
    im = Image.open(PACK / f"{name}.png").convert("RGBA")
    im = im.crop(im.getbbox())
    scale = (CELL - 8) / max(im.size)
    im = im.resize((max(1, round(im.width * scale)), max(1, round(im.height * scale))), Image.LANCZOS)
    x = (i % COLS) * CELL + (CELL - im.width) // 2
    y = (i // COLS) * CELL + (CELL - 2 - im.height)
    atlas.alpha_composite(im, (x, y))
    table.append((name, i, im.width / CELL, im.height / CELL))
OUT.parent.mkdir(parents=True, exist_ok=True)
atlas.save(OUT, optimize=True)
print(f"wrote {OUT} {atlas.size}")
for name, i, w, h in table:
    print(f"        new({i}, {w:.3f}f, {h:.3f}f), // {name}")
