#!/usr/bin/env python3
"""Copies the picked models of Quaternius's Stylized Nature MegaKit (CC0, the Source/Godot
pack) into client/assets/quaternius/nature (naturekit-1), with textures cut down for the
game: colour maps to 1024 px, normal maps to 512. Run from the repo root with the pack
unpacked:

    python3 tools/art/quaternius/import.py <unpacked pack dir> <licence txt>

Needs Pillow. Re-running overwrites the copies; a model the pack lacks keeps the copy
already in the repo (Bush_Common came from the Standard pack). The .gltf files are
rewritten to find their textures in textures/. Add a model by adding its name to MODELS.
"""
import json, os, shutil, sys
from PIL import Image

MODELS = (
    [f"CommonTree_{i}" for i in range(1, 6)] + [f"Pine_{i}" for i in range(1, 6)]
    + [f"TwistedTree_{i}" for i in range(1, 6)] + [f"DeadTree_{i}" for i in range(1, 6)]
    + [f"Birch_{i}" for i in range(1, 6)] + [f"GiantPine_{i}" for i in range(1, 6)]
    + [f"Rock_Medium_{i}" for i in range(1, 5)] + ["Rock_Big_1", "Rock_Big_2"]
    + [f"Pebble_Round_{i}" for i in range(1, 6)]
    + ["Bush_Common", "Bush_Common_Flowers", "Bush_Large", "Bush_Large_Flowers", "Bush_Long_1", "Bush_Long_2",
       "Fern_1", "Fern_2", "Grass_Common_Short", "Grass_Common_Tall", "Grass_Wide_Short", "Grass_Wide_Tall",
       "Grass_Wispy_Short", "Grass_Wispy_Tall", "Flower_3_Group", "Flower_4_Group", "Clover_1", "Clover_2",
       "Plant_1", "Plant_1_Big", "Plant_2", "Plant_3", "Plant_4", "Plant_5", "Plant_6", "Plant_7", "Plant_7_Big",
       "Mushroom_Common", "Mushroom_Laetiporus", "Mushroom_Oyster"]
)
# Textures the kit's shaders read that no model names.
EXTRA = ["Noise_Wind.png", "Noise_Perlin.png"]

def main(kit, licence):
    src = os.path.join(kit, "assets")
    dst = os.path.join("client", "assets", "quaternius", "nature")
    os.makedirs(os.path.join(dst, "textures"), exist_ok=True)
    shutil.copy(licence, dst)
    images = set(EXTRA)
    for name in MODELS:
        path = os.path.join(src, name + ".gltf")
        if not os.path.exists(path):
            print("not in pack, keeping repo copy:", name)
            with open(os.path.join(dst, name + ".gltf")) as f:
                images.update(i["uri"].removeprefix("textures/") for i in json.load(f).get("images", []))
            continue
        shutil.copy(os.path.join(src, name + ".bin"), dst)
        with open(path) as f:
            gltf = json.load(f)
        for image in gltf.get("images", []):
            images.add(image["uri"])
            image["uri"] = "textures/" + image["uri"]
        with open(os.path.join(dst, name + ".gltf"), "w") as f:
            json.dump(gltf, f)
    for uri in sorted(images):
        image = Image.open(os.path.join(src, uri))
        size = 512 if uri.endswith("_Normal.png") or uri.startswith("Noise_") else 1024
        if image.size[0] > size:
            image = image.resize((size, round(image.size[1] * size / image.size[0])), Image.LANCZOS)
        image.save(os.path.join(dst, "textures", uri), optimize=True)
        print(uri, image.size)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
