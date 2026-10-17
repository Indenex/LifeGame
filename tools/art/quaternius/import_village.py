#!/usr/bin/env python3
"""Copies the props Modeling uses from Quaternius's Medieval Village MegaKit (CC0, the
Source/Godot pack) into client/assets/quaternius/village (village-1): crates, wooden
fences, supports, stone rubble and edging, vines and the wagon. House building imports
the kit's building parts into the same folder; textures are shared and cut the same way
(colour 1024 px, normal maps 512; roughness and ORM maps, which our look doesn't read, 64). Run from the repo
root with the pack unpacked:

    python3 tools/art/quaternius/import_village.py <unpacked pack dir> <licence txt>

Needs Pillow. The .gltf files are rewritten to find their textures in textures/.
"""
import json, os, shutil, sys
from PIL import Image

MODELS = (
    ["Prop_Crate", "Prop_WoodenFence_Single", "Prop_WoodenFence_Extension1", "Prop_WoodenFence_Extension2",
     "Prop_Support", "Prop_Wagon", "Prop_ExteriorBorder_Corner", "Prop_ExteriorBorder_Straight1",
     "Prop_ExteriorBorder_Straight2"]
    + [f"Prop_Brick{i}" for i in range(1, 5)] + [f"Prop_Vine{i}" for i in range(1, 10)]
)
# The pack's gltfs name a few textures differently from the files.
RENAMED = {"T_VineLeaf_png.png": "T_VineLeaf.png"}

def main(kit, licence):
    base = os.path.join(kit, "Godot", "addons", "quaternius")
    src = os.path.join(base, "medievalvillagemegakit", "Prop")
    textures = os.path.join(base, "materials")
    dst = os.path.join("client", "assets", "quaternius", "village")
    os.makedirs(os.path.join(dst, "textures"), exist_ok=True)
    shutil.copy(licence, dst)
    images = set()
    for name in MODELS:
        shutil.copy(os.path.join(src, name + ".bin"), dst)
        with open(os.path.join(src, name + ".gltf")) as f:
            gltf = json.load(f)
        for image in gltf.get("images", []):
            uri = RENAMED.get(image["uri"], image["uri"])
            images.add(uri)
            image["uri"] = "textures/" + uri
        with open(os.path.join(dst, name + ".gltf"), "w") as f:
            json.dump(gltf, f)
    for uri in sorted(images):
        image = Image.open(os.path.join(textures, uri))
        # Roughness and ORM maps are kept tiny: the gltf names them, our look doesn't read them.
        size = 64 if "Roughness" in uri or "ORM" in uri else 512 if uri.endswith("_Normal.png") else 1024
        if image.size[0] > size:
            image = image.resize((size, round(image.size[1] * size / image.size[0])), Image.LANCZOS)
        image.save(os.path.join(dst, "textures", uri), optimize=True)
        print(uri, image.size)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
