"""The stone axe, step 1 of 2 (art-rnd-1): run inside Blender.

    blender -b --factory-startup -P tools/art/axe/blender_build.py -- <out dir>

Shape list, in ItemModels' held frame (grip at the origin, the working end towards -Y,
the edge towards +Z, the thin side across X; the same size as ItemModels.StoneAxe but
chunkier, as D-313 asks):
- Head: a knapped stone wedge, a low icosphere pressed into a lens and thinned to an
  edge at +Z, with big flat facets (flat shaded, so each facet is one painted plane).
- Haft: a slightly bent wooden handle with a rounded knob at the grip end.
- Cord: a lashing of chunky cord, wraps round the haft above and below the head and
  two crossed loops over the joint.
One texture. Cycles bakes AO; emission bakes write each texel's position, normal and
part id (.npy floats) for paint.py.

Writes <out>/axe.glb, <out>/axe_{ao.png,pos.npy,nrm.npy,id.npy}.
"""
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
rng = np.random.default_rng(11)


def B(x, y, z):
    return Vector((x, -z, y))


def link(name, me):
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# --- Head: knapped wedge.
HEAD_C = np.array([0.0, -0.47, 0.045])
bm = bmesh.new()
bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0)
for v in bm.verts:
    x, y, z = v.co.x, v.co.z, -v.co.y          # Godot axes of the unit sphere
    t = (z + 1) / 2                             # 0 at the back, 1 at the edge
    thick = 0.042 * (1 - 0.82 * t ** 1.6)       # thin towards the edge
    h = 0.07 * (0.75 + 0.35 * t)                # flares a little towards the edge
    p = np.array([x * thick, y * h, z * 0.115]) + HEAD_C
    # Knapping: push each vertex in or out a little, so facets are big and uneven.
    p[0] += np.sign(x) * (rng.random() - 0.5) * 0.008 * (1 - t)
    v.co = B(*p)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
me = bpy.data.meshes.new("Head")
bm.to_mesh(me)
bm.free()
head = link("Head", me)

# --- Haft: a bent tube with a knob at the grip end.
def tube(name, path, radii, seg=12):
    bm = bmesh.new()
    rings = []
    for i, (p, r) in enumerate(zip(path, radii)):
        a = path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]
        a /= np.linalg.norm(a)
        u = np.cross(a, [0, 0, 1.0])
        u /= np.linalg.norm(u)
        w = np.cross(a, u)
        ring = [bm.verts.new(B(*(p + r * (math.cos(k / seg * 2 * math.pi) * u + math.sin(k / seg * 2 * math.pi) * w)))) for k in range(seg)]
        rings.append(ring)
    for r0, r1 in zip(rings, rings[1:]):
        for k in range(seg):
            bm.faces.new((r0[k], r0[(k + 1) % seg], r1[(k + 1) % seg], r1[k]))
    for ring, rev in ((rings[0], True), (rings[-1], False)):
        c = bm.verts.new(sum((v.co for v in ring), Vector()) / len(ring))
        for k in range(seg):
            f = (ring[k], ring[(k + 1) % seg], c)
            bm.faces.new(f[::-1] if rev else f)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    me.polygons.foreach_set("use_smooth", np.ones(len(me.polygons), bool))
    return link(name, me)


n = 28
ys = np.linspace(0.13, -0.6, n)
path = np.stack([np.zeros(n), ys, 0.006 * np.sin((ys + 0.6) / 0.73 * math.pi)], -1)
radii = []
for y in ys:
    r = 0.026 + 0.002 * (y + 0.6) / 0.73                    # a little thicker at the grip
    knob = max(0.0, 1 - abs(y - 0.11) / 0.03)
    r += 0.008 * knob
    r *= 0.6 + 0.4 * min(1, (0.13 - y) / 0.012) ** 0.5      # rounded end at the grip
    r *= 0.75 + 0.25 * min(1, (y + 0.6) / 0.02) ** 0.5      # rounded end below the head
    radii.append(r)
haft = tube("Haft", path, radii, seg=10)
sub = haft.modifiers.new("sub", "SUBSURF")
sub.levels = 0
dg = bpy.context.evaluated_depsgraph_get()
m2 = bpy.data.meshes.new_from_object(haft.evaluated_get(dg))
haft.modifiers.clear()
haft.data = m2
m2.name = "Haft"
m2.polygons.foreach_set("use_smooth", np.ones(len(m2.polygons), bool))

# --- Cord: wraps and crossed loops, built as curves with a round bevel.
def cord_points(kind):
    pts = []
    if kind in ("above", "below"):
        y0, y1, turns = (-0.405, -0.44, 3) if kind == "above" else (-0.505, -0.545, 3)
        for i in range(turns * 14 + 1):
            t = i / (turns * 14)
            a = t * turns * 2 * math.pi
            pts.append((0.033 * math.cos(a), y0 + (y1 - y0) * t, 0.033 * math.sin(a)))
    else:
        s = 1 if kind == "cross1" else -1
        # A loop round haft and head together, tilted across the joint.
        nrm = np.array([0.0, 0.75, s * 0.66])
        nrm /= np.linalg.norm(nrm)
        u = np.array([1.0, 0, 0])
        w = np.cross(nrm, u)
        for i in range(25):
            a = i / 24 * 2 * math.pi
            p = np.array([0, -0.475, 0.012]) + 0.05 * (math.cos(a) * u * 0.9 + math.sin(a) * w * 1.15)
            pts.append(tuple(p))
    return pts


cords = []
for kind in ("above", "below", "cross1", "cross2"):
    cu = bpy.data.curves.new("c_" + kind, "CURVE")
    cu.dimensions = "3D"
    cu.bevel_depth = 0.0075
    cu.bevel_resolution = 1
    sp = cu.splines.new("POLY")
    pts = cord_points(kind)
    sp.points.add(len(pts) - 1)
    for p, q in zip(sp.points, pts):
        p.co = (*B(*q), 1.0)
    ob = link("c_" + kind, cu)
    cords.append(ob)
dg = bpy.context.evaluated_depsgraph_get()
bm = bmesh.new()
for ob in cords:
    bm.from_mesh(bpy.data.meshes.new_from_object(ob.evaluated_get(dg)))
    bpy.data.objects.remove(ob)
me = bpy.data.meshes.new("Cord")
bm.to_mesh(me)
bm.free()
me.polygons.foreach_set("use_smooth", np.ones(len(me.polygons), bool))
cord = link("Cord", me)

parts = {"Head": (head, 1), "Haft": (haft, 2), "Cord": (cord, 3)}

# --- UVs: one atlas; the head gets extra room (it carries the most painting).
bpy.ops.object.select_all(action="DESELECT")
for ob, _ in parts.values():
    ob.select_set(True)
bpy.context.view_layer.objects.active = head
head.scale = (2.2, 2.2, 2.2)   # temporary: smart_project packs by area, so this buys texels
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=math.radians(50), island_margin=0.006, scale_to_bounds=False)
bpy.ops.object.mode_set(mode="OBJECT")
head.scale = (1, 1, 1)

# --- Bakes.
images = {}
for kind in ("ao", "pos", "nrm", "id"):
    img = bpy.data.images.new(f"axe_{kind}", SIZE, SIZE, alpha=True, float_buffer=(kind != "ao"))
    if kind != "ao":
        img.colorspace_settings.name = "Non-Color"
    images[kind] = img
mats = {}
for name, (ob, pid) in parts.items():
    mat = bpy.data.materials.new("bake_" + name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    tex = nt.nodes.new("ShaderNodeTexImage")
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    rgb = nt.nodes.new("ShaderNodeRGB")
    rgb.outputs[0].default_value = (pid / 16.0, 0, 0, 1)
    em = nt.nodes.new("ShaderNodeEmission")
    o = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], o.inputs[0])
    nt.nodes.active = tex
    ob.data.materials.append(mat)
    mats[name] = (mat, tex, geo, rgb, em)

scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.render.bake.use_selected_to_active = False
scene.render.bake.margin = 8
scene.render.bake.margin_type = "EXTEND"
bpy.ops.object.select_all(action="DESELECT")
for ob, _ in parts.values():
    ob.select_set(True)
bpy.context.view_layer.objects.active = head
for kind in ("ao", "pos", "nrm", "id"):
    for mat, tex, geo, rgb, em in mats.values():
        tex.image = images[kind]
        for l in list(em.inputs[0].links):
            mat.node_tree.links.remove(l)
        src = {"pos": geo.outputs["Position"], "nrm": geo.outputs["Normal"], "id": rgb.outputs[0]}.get(kind)
        if src is not None:
            mat.node_tree.links.new(src, em.inputs[0])
    scene.cycles.samples = 128 if kind == "ao" else 1
    bpy.ops.object.bake(type="AO" if kind == "ao" else "EMIT")
    img = images[kind]
    if kind == "ao":
        img.filepath_raw = os.path.join(out, "axe_ao.png")
        img.file_format = "PNG"
        img.save()
    else:
        px = np.empty(SIZE * SIZE * 4, np.float32)
        img.pixels.foreach_get(px)
        np.save(os.path.join(out, f"axe_{kind}.npy"), px.reshape(SIZE, SIZE, 4)[::-1].copy())
    print("baked", kind)

bpy.ops.object.select_all(action="DESELECT")
tris = {}
for name, (ob, _) in parts.items():
    ob.data.materials.clear()
    ob.select_set(True)
    ob.data.update()
    tris[name] = sum(len(p.vertices) - 2 for p in ob.data.polygons)
bpy.ops.export_scene.gltf(
    filepath=os.path.join(out, "axe.glb"), use_selection=True, export_format="GLB",
    export_texcoords=True, export_normals=True, export_vertex_color="NONE",
    export_materials="NONE", export_yup=True)
print("exported axe.glb tris", tris, "total", sum(tris.values()))
