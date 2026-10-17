"""The sheep, step 1 of 2 (art-rnd-1): run inside Blender.

    blender -b --factory-startup -P tools/art/sheep/blender_build.py -- <out dir>

Shape list (sizes match SheepFigure: Center (0, 0.49, 0), HeadAt (0, 0.64, 0.42), hips
at y 0.3, x +-0.15, z +-0.25, so Sheep.cs's bone poses fit unchanged):
- Fleece: a soft cloud of chunky wool puffs (spheres on an ellipsoid core), voxel
  remeshed into one surface and smoothed so the lobes run together.
- Tuft: a little cloud of wool on the head (rides the head bone).
- Head: a round dark head with a pale muzzle, two soft eye domes and little drooping
  ears, one remeshed surface.
- Body: the pink skin under the fleece (seen when shorn; the fleece bone shrinks).
- Leg0..Leg3: short chunky legs with hooves, one per hip, in SheepFigure.Hips order.

Every part is a separate named mesh in model space (no node transforms); the C# side
weights each to its bone. Two textures: wool (Fleece, Tuft) and skin (the rest).
For each, Cycles bakes AO, and emission bakes write the surface position, normal and
part id per texel (as .npy floats), which paint.py paints from.

Writes <out>/sheep.glb, <out>/<tex>_{ao.png,pos.npy,nrm.npy,id.npy}, <out>/layout.json.
"""
import json
import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

out = os.path.abspath(sys.argv[sys.argv.index("--") + 1]) if "--" in sys.argv else os.getcwd()
os.makedirs(out, exist_ok=True)
SIZE = 1024


def B(x, y, z):
    # Godot (x, y up, z forward) -> Blender (x, -z, y up); the glTF exporter turns it back.
    return Vector((x, -z, y))


CENTER = np.array([0.0, 0.49, 0.0])
CORE = np.array([0.3, 0.25, 0.43])
HEAD = np.array([0.0, 0.64, 0.42])
HIPS = [(-0.15, 0.3, 0.25), (0.15, 0.3, 0.25), (-0.15, 0.3, -0.25), (0.15, 0.3, -0.25)]
rng = np.random.default_rng(7)


def sphere(bm, at, radii, rot=None, seg=24, rings=14):
    m = Matrix.Translation(B(*at)) @ (rot or Matrix.Identity(4)) @ Matrix.Diagonal((radii[0], radii[2], radii[1], 1.0))
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0, matrix=m)


def blob(name, build, voxel, smooth_iters, decimate, smooth_factor=0.6):
    """Union primitives into one surface: voxel remesh, smooth, decimate."""
    bm = bmesh.new()
    build(bm)
    me = bpy.data.meshes.new(name + "_src")
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    r = ob.modifiers.new("remesh", "REMESH")
    r.mode, r.voxel_size, r.adaptivity = "VOXEL", voxel, 0.0
    if smooth_iters:
        s = ob.modifiers.new("smooth", "SMOOTH")
        s.factor, s.iterations = smooth_factor, smooth_iters
    if decimate < 1:
        d = ob.modifiers.new("decimate", "DECIMATE")
        d.ratio = decimate
    dg = bpy.context.evaluated_depsgraph_get()
    final = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    ob.modifiers.clear()
    ob.data = final
    final.name = name
    final.polygons.foreach_set("use_smooth", np.ones(len(final.polygons), bool))
    return ob


bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# --- Fleece: chunky puffs over a soft core. Fibonacci points on the core ellipsoid,
# lifted a little so the back is a heaped cloud; fewer, bigger puffs read as cute.
puffs = []
count = 34
for i in range(count):
    y = 1 - (i + 0.5) / count * 1.75          # skip the very bottom (legs, belly)
    rr = math.sqrt(max(0.0, 1 - y * y))
    a = i * 2.39996 + 0.4
    d = np.array([math.cos(a) * rr, y, math.sin(a) * rr])
    p = CENTER + d * CORE * 0.86
    r = 0.115 + 0.03 * rng.random() + 0.02 * max(0, y)   # bigger puffs on top
    puffs.append((p, r))
# A rump puff (tail) and a collar of wool round the neck.
puffs.append((CENTER + np.array([0, 0.02, -0.45]), 0.1))
for s in (-1, 0, 1):
    puffs.append((CENTER + np.array([s * 0.13, 0.08 - abs(s) * 0.04, 0.38]), 0.12))


def build_fleece(bm):
    sphere(bm, CENTER, CORE * 0.92)
    for p, r in puffs:
        sphere(bm, p, (r, r * 0.92, r), seg=16, rings=10)


fleece = blob("Fleece", build_fleece, 0.011, 4, 0.055, 0.5)

# --- Tuft: a few small puffs on the crown.
tuft = [(HEAD + np.array([0, 0.13, 0.07]), 0.065), (HEAD + np.array([-0.055, 0.115, 0.03]), 0.055),
        (HEAD + np.array([0.055, 0.115, 0.03]), 0.055), (HEAD + np.array([0, 0.115, 0.14]), 0.048)]


def build_tuft(bm):
    for p, r in tuft:
        sphere(bm, p, (r, r * 0.85, r), seg=14, rings=8)


tuft_ob = blob("Tuft", build_tuft, 0.007, 3, 0.09, 0.5)

# --- Head: round skull, a pale muzzle, eye domes, little ears drooping to the sides.
SKULL = HEAD + np.array([0, 0.01, 0.11])
SKULL_R = np.array([0.135, 0.128, 0.145])
MUZZLE = HEAD + np.array([0, -0.045, 0.23])
MUZZLE_R = np.array([0.09, 0.074, 0.078])
eyes = []
for side in (-1, 1):
    d = np.array([side * 0.62, 0.22, 0.75])
    d /= np.linalg.norm(d)
    eyes.append(SKULL + d * SKULL_R * 0.97)
ears = []
for side in (-1, 1):
    ears.append(HEAD + np.array([side * 0.175, 0.045, 0.08]))


def build_head(bm):
    sphere(bm, SKULL, SKULL_R)
    sphere(bm, MUZZLE, MUZZLE_R)
    for e in eyes:
        sphere(bm, e, (0.033, 0.038, 0.024), seg=12, rings=8)
    for side, e in zip((-1, 1), ears):
        # Droop: turn about the forward axis (Blender -Y).
        rot = Matrix.Rotation(side * 0.5, 4, "Y")
        sphere(bm, e, (0.075, 0.026, 0.042), rot=rot, seg=14, rings=8)
    # Neck into the fleece, so no gap shows when the head turns.
    sphere(bm, HEAD + np.array([0, -0.02, -0.02]), (0.08, 0.09, 0.1))


head = blob("Head", build_head, 0.006, 3, 0.07, 0.5)

# --- Body under the fleece (pink skin).
body = blob("Body", lambda bm: sphere(bm, CENTER + np.array([0, -0.02, 0]), CORE * 0.8, seg=16, rings=10), 0.02, 0, 0.07)

# --- Legs: short chunky tapered capsules ending in a slightly wider hoof.
legs = []
for i, (hx, hy, hz) in enumerate(HIPS):
    def build_leg(bm, hx=hx, hy=hy, hz=hz):
        top, n = hy + 0.05, 7
        for k in range(n):
            t = k / (n - 1)
            y = top + (0.035 - top) * t
            r = 0.058 + (0.046 - 0.058) * t
            sphere(bm, (hx, y, hz + 0.01 * t), (r, r, r), seg=12, rings=8)
        sphere(bm, (hx, 0.03, hz + 0.012), (0.054, 0.032, 0.06), seg=12, rings=8)
    legs.append(blob(f"Leg{i}", build_leg, 0.008, 3, 0.07, 0.5))

parts = {"Fleece": (fleece, "wool", 1), "Tuft": (tuft_ob, "wool", 2), "Head": (head, "skin", 3), "Body": (body, "skin", 4)}
for i, leg in enumerate(legs):
    parts[f"Leg{i}"] = (leg, "skin", 5 + i)

# --- UVs: one atlas per texture, islands packed by area.
for group in ("wool", "skin"):
    obs = [o for o, g, _ in parts.values() if g == group]
    bpy.ops.object.select_all(action="DESELECT")
    for o in obs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.004, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode="OBJECT")

# --- Bakes.
images = {}
for group in ("wool", "skin"):
    for kind in ("ao", "pos", "nrm", "id"):
        img = bpy.data.images.new(f"{group}_{kind}", SIZE, SIZE, alpha=True, float_buffer=(kind != "ao"))
        if kind != "ao":
            img.colorspace_settings.name = "Non-Color"
        images[(group, kind)] = img

mats = {}
for name, (ob, group, pid) in parts.items():
    mat = bpy.data.materials.new("bake_" + name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    tex = nt.nodes.new("ShaderNodeTexImage")
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    rgb = nt.nodes.new("ShaderNodeRGB")
    rgb.outputs[0].default_value = (pid / 16.0, 0, 0, 1)
    em = nt.nodes.new("ShaderNodeEmission")
    outn = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], outn.inputs[0])
    nt.nodes.active = tex
    ob.data.materials.append(mat)
    mats[name] = (mat, tex, geo, rgb, em, group)

scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 48
scene.render.bake.use_selected_to_active = False
scene.render.bake.margin = 8
scene.render.bake.margin_type = "EXTEND"
scene.view_settings.view_transform = "Standard"

for group in ("wool", "skin"):
    names = [n for n, v in mats.items() if v[5] == group]
    bpy.ops.object.select_all(action="DESELECT")
    for n in names:
        parts[n][0].select_set(True)
    bpy.context.view_layer.objects.active = parts[names[0]][0]
    for kind in ("ao", "pos", "nrm", "id"):
        for n in names:
            mat, tex, geo, rgb, em, _ = mats[n]
            tex.image = images[(group, kind)]
            for l in list(em.inputs[0].links):
                mat.node_tree.links.remove(l)
            src = {"pos": geo.outputs["Position"], "nrm": geo.outputs["Normal"], "id": rgb.outputs[0]}.get(kind)
            if src is not None:
                mat.node_tree.links.new(src, em.inputs[0])
        if kind == "ao":
            scene.cycles.samples = 64
            bpy.ops.object.bake(type="AO")
        else:
            scene.cycles.samples = 1
            bpy.ops.object.bake(type="EMIT")
        img = images[(group, kind)]
        if kind == "ao":
            img.filepath_raw = os.path.join(out, f"{group}_ao.png")
            img.file_format = "PNG"
            img.save()
        else:
            px = np.empty(SIZE * SIZE * 4, np.float32)
            img.pixels.foreach_get(px)
            np.save(os.path.join(out, f"{group}_{kind}.npy"), px.reshape(SIZE, SIZE, 4)[::-1].copy())
        print("baked", group, kind)

json.dump({
    "puffs": [[*map(float, p), float(r)] for p, r in puffs],
    "tuft": [[*map(float, p), float(r)] for p, r in tuft],
    "eyes": [list(map(float, e)) for e in eyes],
    "ears": [list(map(float, e)) for e in ears],
    "skull": [*map(float, SKULL), *map(float, SKULL_R)],
    "muzzle": [*map(float, MUZZLE), *map(float, MUZZLE_R)],
    "center": list(map(float, CENTER)), "head": list(map(float, HEAD)),
}, open(os.path.join(out, "layout.json"), "w"))

# --- Export the game meshes.
bpy.ops.object.select_all(action="DESELECT")
tris = {}
for name, (ob, _, _) in parts.items():
    ob.data.materials.clear()
    ob.select_set(True)
    tris[name] = sum(len(p.vertices) - 2 for p in ob.data.polygons)
bpy.ops.export_scene.gltf(
    filepath=os.path.join(out, "sheep.glb"), use_selection=True, export_format="GLB",
    export_texcoords=True, export_normals=True, export_vertex_color="NONE",
    export_materials="NONE", export_yup=True)
print("exported sheep.glb tris", tris, "total", sum(tris.values()))
