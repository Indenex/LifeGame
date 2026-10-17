#!/usr/bin/env python3
"""Copies the picked models of Quaternius's Stylized Nature MegaKit (CC0) into
client/assets/naturekit (naturekit-1), with textures cut down for the game: colour maps
to 1024 px, normal maps to 512. Run from the repo root with the kit's zip unpacked:

    python3 tools/art/naturekit/import.py <unpacked kit dir>

Needs Pillow. Re-running overwrites the copies. The .gltf files are rewritten to find
their textures in textures/. Add a model by adding its name to MODELS.
"""
import glob, json, os, shutil, sys
from PIL import Image

MODELS = (
    [f"CommonTree_{i}" for i in range(1, 6)] + [f"Pine_{i}" for i in range(1, 6)]
    + [f"TwistedTree_{i}" for i in range(1, 6)] + [f"DeadTree_{i}" for i in range(1, 6)]
    + [f"Rock_Medium_{i}" for i in range(1, 4)] + [f"Pebble_Round_{i}" for i in range(1, 6)]
    + ["Bush_Common", "Bush_Common_Flowers", "Fern_1", "Grass_Common_Short", "Grass_Common_Tall",
       "Grass_Wispy_Short", "Grass_Wispy_Tall", "Flower_3_Group", "Flower_4_Group", "Clover_1", "Clover_2",
       "Plant_1", "Plant_1_Big", "Plant_7", "Plant_7_Big", "Mushroom_Common", "Mushroom_Laetiporus"]
)

def main(kit):
    src = os.path.join(kit, "glTF")
    dst = os.path.join("client", "assets", "naturekit")
    os.makedirs(os.path.join(dst, "textures"), exist_ok=True)
    shutil.copy(os.path.join(kit, "License_Standard.txt"), dst)
    images = set()
    for name in MODELS:
        shutil.copy(os.path.join(src, name + ".bin"), dst)
        with open(os.path.join(src, name + ".gltf")) as f:
            gltf = json.load(f)
        for image in gltf.get("images", []):
            images.add(image["uri"])
            image["uri"] = "textures/" + image["uri"]
        with open(os.path.join(dst, name + ".gltf"), "w") as f:
            json.dump(gltf, f)
    for uri in sorted(images):
        image = Image.open(os.path.join(src, uri))
        size = 512 if uri.endswith("_Normal.png") else 1024
        if image.size[0] > size:
            image = image.resize((size, round(image.size[1] * size / image.size[0])), Image.LANCZOS)
        image.save(os.path.join(dst, "textures", uri), optimize=True)
        print(uri, image.size)

if __name__ == "__main__":
    main(sys.argv[1])
