"""Painted trees, step 1 of 2 (art-rnd-1): run inside Blender.

    blender -b --factory-startup -P tools/art/trees/blender_build.py -- <out dir> <birch|pine>

Builds one tree as a game mesh in Godot units (Y up, standing about 4.4 m tall like
PaintedTrees.Height), unwraps it, and bakes with Cycles on the CPU what paint.py needs to
paint the albedo in 3D (so the strokes run across UV seams): ambient occlusion, the
object-space normal, the position, and a wood/leaf mask. Exports <out>/<tree>.glb with
vertex colour RGB white and alpha = wind sway weight (0 at the trunk base, 1 at the crown
tips), and <out>/<tree>_bake.npz.

birch: a white, gently leaning trunk with a few branches, and a round crown of about a
dozen chunky leaf clumps. Each clump is a ball with a few soft sculpted lobes and a
flatter underside; the clumps are fused into one surface with a voxel remesh (so the
crevices between them are real geometry for the AO and the silhouette), smoothed, and
decimated to the game budget. A few clumps are left out low on the crown, so gaps show
the branches.

pine: five stacked tiers, each one closed, puffy skirt with a rolled hem and rounded
scallops whose tips droop lower than the notches, on a short warm-brown trunk.
"""
import math
import os
import sys

import bpy
import numpy as np

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
out = os.path.abspath(argv[0] if argv else os.getcwd())
TREE = argv[1] if len(argv) > 1 else "birch"
os.makedirs(out, exist_ok=True)
SIZE = 1024
CROWN_TRIS = 5600


def to_blender(v):
    # Godot (x, y up, z forward) -> Blender (x, -z, y up); the glTF exporter turns it back.
    v = np.asarray(v, float)
    return np.stack([v[:, 0], -v[:, 2], v[:, 1]], -1)


def from_blender(v):
    v = np.asarray(v, float)
    return np.stack([v[..., 0], v[..., 2], -v[..., 1]], -1)


def make_object(name, verts, faces):
    me = bpy.data.meshes.new(name)
    me.from_pydata(to_blender(verts).tolist(), [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def frames(path):
    """Tangent-following frames along a polyline (parallel transport)."""
    t = np.gradient(path, axis=0)
    t /= np.linalg.norm(t, axis=1, keepdims=True)
    ref = np.array([1.0, 0, 0]) if abs(t[0][0]) < 0.9 else np.array([0, 0, 1.0])
    us, vs = [], []
    u = ref - t[0] * np.dot(ref, t[0])
    u /= np.linalg.norm(u)
    for ti in t:
        u = u - ti * np.dot(u, ti)
        u /= np.linalg.norm(u)
        us.append(u)
        vs.append(np.cross(ti, u))
    return np.array(us), np.array(vs)


def tube(path, radii, sides, cap_end=True):
    """A closed tube along path (Godot space): verts, faces (quads, with end caps)."""
    path = np.asarray(path, float)
    us, vs = frames(path)
    a = np.linspace(0, 2 * np.pi, sides, endpoint=False)
    verts = []
    for p, u, v, r in zip(path, us, vs, radii):
        verts += list(p + r * (np.cos(a)[:, None] * u + np.sin(a)[:, None] * v))
    faces = []
    n = len(path)
    for i in range(n - 1):
        for j in range(sides):
            k = (j + 1) % sides
            faces.append((i * sides + j, i * sides + k, (i + 1) * sides + k, (i + 1) * sides + j))
    base = len(verts)
    verts.append(path[0] - (path[1] - path[0]) * 0.2)
    faces += [(base, (j + 1) % sides, j) for j in range(sides)]
    if cap_end:
        verts.append(path[-1] + (path[-1] - path[-2]) * 0.5)
        top = base + 1
        faces += [(top, (n - 1) * sides + j, (n - 1) * sides + (j + 1) % sides) for j in range(sides)]
    return np.array(verts), faces


def icosphere(subdiv):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv, radius=1.0)
    ob = bpy.context.active_object
    me = ob.data
    v = np.zeros(len(me.vertices) * 3)
    me.vertices.foreach_get("co", v)
    faces = [tuple(p.vertices) for p in me.polygons]
    bpy.data.objects.remove(ob)
    return from_blender(v.reshape(-1, 3)), faces


def apply_modifier(ob, kind, **props):
    m = ob.modifiers.new(kind.lower(), kind)
    for k, val in props.items():
        setattr(m, k, val)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=m.name)


def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
rng = np.random.default_rng(7 if TREE == "birch" else 11)
wood_parts = []          # (verts, faces) in Godot space

if TREE == "birch":
    # The trunk: white, slender, leaning gently to +x with a soft S, a little root flare.
    TOP = 3.0
    ys = np.linspace(0, TOP, 24)
    path = np.stack([0.24 * (ys / TOP) ** 1.7, ys, 0.06 * np.sin(ys * 1.3)], -1)
    radii = 0.085 + 0.08 * (1 - ys / TOP) ** 1.2 + 0.08 * np.exp(-ys / 0.16)
    wood_parts.append(tube(path, radii, 12))

    def trunk_at(y):
        return np.array([0.24 * (y / TOP) ** 1.7, y, 0.06 * np.sin(y * 1.3)])

    # The crown: clumps on an egg-shaped envelope round the crown's heart.
    heart = np.array([0.2, 2.78, 0.0])
    env = np.array([1.0, 1.0, 1.0])
    clumps = [(heart + np.array([0.02, 1.0, 0.0]), 0.66)]
    golden = np.pi * (3 - np.sqrt(5))
    n = 15
    for i in range(n):
        y = 0.82 - 1.55 * (i + 0.5) / n
        r = np.sqrt(max(0.0, 1 - y * y))
        a = i * golden + 0.4
        d = np.array([r * np.cos(a), y, r * np.sin(a)])
        # Leave a couple of low clumps out, so gaps show the branches.
        if i in (11, 13):
            continue
        rad = rng.uniform(0.5, 0.66) * (1.0 if y > -0.3 else 0.88)
        clumps.append((heart + d * env * rng.uniform(0.86, 1.0), rad))
    # Two inner clumps hold the middle together.
    clumps.append((heart + np.array([0.1, -0.05, 0.1]), 0.7))
    clumps.append((heart + np.array([-0.15, 0.3, -0.1]), 0.65))

    # Branches from the trunk up into the lower and outer clumps.
    for i, (c, rad) in enumerate(clumps[1:14:2]):
        y0 = 1.8 + 0.2 * (i % 4)
        p0 = trunk_at(y0)
        ctrl = p0 + np.array([0, (c[1] - p0[1]) * 0.6 + 0.2, 0])
        t = np.linspace(0, 1, 8)[:, None]
        end = c - (c - heart) * 0.25
        bez = (1 - t) ** 2 * p0 + 2 * (1 - t) * t * ctrl + t * t * end
        wood_parts.append(tube(bez, np.linspace(0.06, 0.022, 8), 7))

    sv, sf = icosphere(5)
    shapes = []
    for c, rad in clumps:
        d = sv.copy()
        # A few soft lobes on the top and outer side, so each clump is a sculpted
        # cauliflower-ish ball instead of a sphere.
        bump = np.zeros(len(d))
        out_dir = (c - heart) / (np.linalg.norm(c - heart) + 1e-6)
        for _ in range(7):
            s = rng.normal(size=3)
            s = s / np.linalg.norm(s) + out_dir * 0.6 + np.array([0, 0.5, 0])
            s /= np.linalg.norm(s)
            bump += rng.uniform(0.1, 0.2) * np.exp((d @ s - 1) / rng.uniform(0.06, 0.11))
        r = rad * (0.92 + bump)
        p = d * r[:, None]
        # Flatter, tucked-in underside; a little squash overall.
        p[:, 1] = np.where(p[:, 1] < 0, p[:, 1] * 0.62, p[:, 1] * 0.92)
        shapes.append((p + c, sf))
    verts = np.concatenate([s[0] for s in shapes])
    faces, off = [], 0
    for p, f in shapes:
        faces += [tuple(i + off for i in face) for face in f]
        off += len(p)
    crown = make_object("Crown", verts, faces)
    # Fuse the clumps into one surface, soften, and bring it down to the game budget.
    apply_modifier(crown, "REMESH", mode="VOXEL", voxel_size=0.03)
    apply_modifier(crown, "SMOOTH", factor=0.6, iterations=6)
    apply_modifier(crown, "DECIMATE", ratio=min(1.0, CROWN_TRIS / max(1, tri_count(crown))))
    crown_tris = tri_count(crown)
    H_TOP, R_CROWN, AXIS = 4.4, 1.7, heart
else:
    # The trunk: short, warm brown, with a root flare.
    TOP = 2.6
    ys = np.linspace(0, TOP, 14)
    path = np.stack([0.03 * (ys / TOP) ** 2, ys, np.zeros_like(ys)], -1)
    radii = 0.07 + 0.07 * (1 - ys / TOP) + 0.09 * np.exp(-ys / 0.14)
    wood_parts.append(tube(path, radii, 10))

    # (hem y, apex y, radius, lobes)
    tiers = [(0.82, 2.05, 1.16, 9), (1.5, 2.7, 0.98, 8), (2.18, 3.3, 0.79, 7), (2.84, 3.86, 0.6, 6), (3.42, 4.42, 0.4, 5)]
    # Profile of one tier as (radius fraction, height fraction above the hem) from the
    # apex down the puffy top, round the rolled hem, and back in along the underside.
    prof = np.array([
        (0.0, 1.0), (0.12, 0.9), (0.3, 0.7), (0.5, 0.5), (0.68, 0.33), (0.83, 0.18), (0.94, 0.07),
        (1.0, -0.03), (0.97, -0.12), (0.87, -0.15), (0.68, -0.09), (0.44, 0.0), (0.2, 0.1), (0.0, 0.16),
    ])
    verts, faces = [], []
    for ti, (hem, apex, R, lobes) in enumerate(tiers):
        h = apex - hem
        segs = lobes * 6
        theta = np.linspace(0, 2 * np.pi, segs, endpoint=False) + rng.uniform(0, 2 * np.pi)
        u = theta * lobes / (2 * np.pi)
        b = np.abs(np.sin(np.pi * u)) ** 0.55       # 1 at a scallop's tip, 0 in a notch
        k = np.floor(u).astype(int) % lobes
        var = rng.uniform(-1, 1, lobes)[k]
        base = len(verts)
        rows = len(prof)
        for (rf, yf) in prof:
            w = rf ** 2
            m = (1 - 0.2 * (1 - b) * w) * (1 + 0.07 * var * b * w)
            droop = (0.13 + 0.04 * var) * h * b * rf ** 3
            for j in range(segs):
                r = R * rf * m[j]
                verts.append((r * np.cos(theta[j]), hem + yf * h - droop[j], r * np.sin(theta[j])))
        for i in range(rows - 1):
            for j in range(segs):
                jn = (j + 1) % segs
                a0, a1 = base + i * segs + j, base + i * segs + jn
                b0, b1 = base + (i + 1) * segs + j, base + (i + 1) * segs + jn
                if i == 0:
                    faces.append((a0, b1, b0))           # the apex is a pole
                elif i == rows - 2:
                    faces.append((a0, a1, b0))           # so is the underside's middle
                else:
                    faces.append((a0, a1, b1, b0))
    verts = np.array(verts)
    # A slight lean.
    verts[:, 0] += 0.04 * (verts[:, 1] / 4.4) ** 2
    crown = make_object("Crown", verts, faces)
    bpy.context.view_layer.objects.active = crown
    crown.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=1e-5)
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    crown_tris = tri_count(crown)
    H_TOP, R_CROWN, AXIS = 4.42, 1.2, np.array([0.0, 0.0, 0.0])

# The wood as one object.
wv, wf, off = [], [], 0
for v, f in wood_parts:
    wv.append(v)
    wf += [tuple(i + off for i in face) for face in f]
    off += len(v)
wood = make_object("Wood", np.concatenate(wv), wf)
wood_tris = tri_count(wood)
# The leaf/wood mask, carried through the join as a point attribute.
for ob, val in ((crown, 1.0), (wood, 0.0)):
    attr = ob.data.attributes.new("leaf", "FLOAT", "POINT")
    attr.data.foreach_set("value", np.full(len(ob.data.vertices), val))
bpy.ops.object.select_all(action="DESELECT")
crown.select_set(True)
wood.select_set(True)
bpy.context.view_layer.objects.active = crown
bpy.ops.object.join()
tree = bpy.context.active_object
tree.name = TREE
me = tree.data
me.polygons.foreach_set("use_smooth", np.ones(len(me.polygons), bool))

# Unwrap: smart projection, islands packed by area (the painting is in 3D, so seams
# don't matter; only even texel density does).
bpy.ops.object.select_all(action="DESELECT")
tree.select_set(True)
bpy.context.view_layer.objects.active = tree
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.006, area_weight=0.0, scale_to_bounds=False)
bpy.ops.object.mode_set(mode="OBJECT")

# Wind sway weight per vertex: 0 at the trunk base, rising with height (squared, so the
# trunk barely moves) and with distance out from the crown's axis, 1 at the crown tips.
co = np.zeros(len(me.vertices) * 3)
me.vertices.foreach_get("co", co)
gp = from_blender(co.reshape(-1, 3))
horiz = np.hypot(gp[:, 0] - AXIS[0], gp[:, 2] - AXIS[2])
print("bounds", gp.min(0).round(2), gp.max(0).round(2))
sway = np.clip(0.78 * (gp[:, 1] / H_TOP) ** 2 + 0.3 * np.clip(horiz / R_CROWN, 0, 1), 0, 1)
me.color_attributes.new("Color", "FLOAT_COLOR", "POINT")
me.color_attributes["Color"].data.foreach_set("color", np.stack([np.ones_like(sway)] * 3 + [sway], -1).ravel())
me.color_attributes.active_color = me.color_attributes["Color"]

# A ground plane, so the AO darkens the trunk's foot.
bpy.ops.mesh.primitive_plane_add(size=12, location=(0, 0, 0))
ground = bpy.context.active_object

# Bakes: AO, object-space normal, and two emission bakes for the position and the mask.
world = bpy.data.worlds.new("w")
scene.world = world
world.light_settings.distance = 0.9
scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 256   # a clean AO: no grain in the painting
scene.render.bake.use_selected_to_active = False
scene.render.bake.margin = 8
scene.render.bake.normal_space = "OBJECT"

mat = bpy.data.materials.new("bake")
mat.use_nodes = True
nodes, links = mat.node_tree.nodes, mat.node_tree.links
for nd in list(nodes):
    nodes.remove(nd)
outn = nodes.new("ShaderNodeOutputMaterial")
emit = nodes.new("ShaderNodeEmission")
geo = nodes.new("ShaderNodeNewGeometry")
attr = nodes.new("ShaderNodeAttribute")
attr.attribute_name = "leaf"
links.new(emit.outputs["Emission"], outn.inputs["Surface"])
tex = nodes.new("ShaderNodeTexImage")
nodes.active = tex
me.materials.append(mat)

bpy.ops.object.select_all(action="DESELECT")
tree.select_set(True)
bpy.context.view_layer.objects.active = tree
maps = {}
for name, kind, src in (("ao", "AO", None), ("normal", "NORMAL", None), ("pos", "EMIT", "pos"), ("leaf", "EMIT", "leaf")):
    img = bpy.data.images.new(name, SIZE, SIZE, alpha=True, float_buffer=True)
    img.generated_color = (0, 0, 0, 0)
    tex.image = img
    for l in list(emit.inputs["Color"].links):
        links.remove(l)
    if src == "pos":
        links.new(geo.outputs["Position"], emit.inputs["Color"])
    elif src == "leaf":
        links.new(attr.outputs["Fac"], emit.inputs["Color"])
    bpy.ops.object.bake(type=kind)
    px = np.zeros(SIZE * SIZE * 4, np.float32)
    img.pixels.foreach_get(px)
    maps[name] = px.reshape(SIZE, SIZE, 4)[::-1]      # row 0 = top (v = 1), as in a PNG
    print("baked", name)
np.savez_compressed(os.path.join(out, f"{TREE}_bake.npz"), **maps)

# Export only the tree, without its bake material.
me.materials.clear()
me.validate()
bpy.data.objects.remove(ground)
bpy.ops.object.select_all(action="DESELECT")
tree.select_set(True)
bpy.context.view_layer.objects.active = tree
bpy.ops.export_scene.gltf(
    filepath=os.path.join(out, f"{TREE}.glb"),
    use_selection=True,
    export_format="GLB",
    export_texcoords=True,
    export_normals=True,
    export_vertex_color="ACTIVE",
    export_materials="NONE",
    export_yup=True,
)
print(f"exported {TREE}.glb: {len(me.vertices)} verts, {tri_count(tree)} tris (crown {crown_tris}, wood {wood_tris})")
