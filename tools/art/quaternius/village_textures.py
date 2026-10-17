#!/usr/bin/env python3
# Imports the Quaternius Medieval Village MegaKit's textures (CC0) into
# client/assets/quaternius/village/textures/ (building-26; the layout Modeling uses
# for the nature kit too): base colours at 1024 px, normal maps at 512 px, plus the
# wood grain bands House building lays along sticks and logs.
#
#   python3 -I tools/art/quaternius/village_textures.py <villagekit-godot.zip>
#
# Needs Pillow. The licence goes next to the textures as License_Village_Source.txt.
import io, os, sys, zipfile
from PIL import Image, ImageOps

KIT = "Godot/addons/quaternius/materials/"
BASE = ["T_WoodTrim_BaseColor", "T_Plaster_BaseColor", "T_RoundRocks_BaseColor", "T_RockTrim_BaseColor",
        "T_UnevenBrick_BaseColor", "T_Brick_BaseColor", "T_RoundTiles_BaseColor", "T_FlatTiles_BaseColor"]
NORMAL = ["T_WoodTrim_Normal", "T_Plaster_Normal", "T_RoundRocks_Normal", "T_RockTrim_Normal", "T_UnevenBrick_Normal"]

def main(zip_path):
    out = os.path.join(os.path.dirname(__file__), "../../../client/assets/quaternius/village/textures")
    os.makedirs(out, exist_ok=True)
    with zipfile.ZipFile(zip_path) as z:
        def load(name):
            return Image.open(io.BytesIO(z.read(KIT + name + ".png")))
        for name in BASE:
            load(name).convert("RGB").resize((1024, 1024), Image.LANCZOS).save(f"{out}/{name}.png", optimize=True)
        for name in NORMAL:
            load(name).convert("RGB").resize((512, 512), Image.LANCZOS).save(f"{out}/{name}.png", optimize=True)
        # The trim sheet's two plank bands, each mirrored into a tile that repeats both ways:
        # grain runs along x, so a stick's UVs (metres along it, then round it) follow the grain.
        wood = load("T_WoodTrim_BaseColor").convert("RGB")
        for name, (y0, y1) in {"wood_light": (8, 612), "wood_dark": (648, 1252)}.items():
            band = wood.crop((0, y0, 2048, y1)).resize((1024, 256), Image.LANCZOS)
            tile = Image.new("RGB", (1024, 512))
            tile.paste(band, (0, 0))
            tile.paste(ImageOps.flip(band), (0, 256))
            tile.save(f"{out}/{name}.png", optimize=True)
    print("textures in", os.path.normpath(out))

if __name__ == "__main__":
    main(sys.argv[1])
