"""Bramble's and Pebble's heads, step 1 of 2 (art-rnd-1): run inside Blender.

    blender -b --factory-startup -P tools/art/heads/blender_build.py -- <out dir> [--preview]

Models both heads in the chibi rig's rest space (ChibiFigure: 1.35 m tall, 2.8 heads
high, the head centred 1.109 m up), so they hang on HumanRig.Head under the inverse of
the bone's rest, as ChibiShell does on Spine:

- Bramble: a soft round skin head and a mop of chunky hair clumps (metaballs: a cap,
  a swept fringe, side flicks, nape locks and the crown tuft), melted into one mesh.
- Pebble: a smooth turtle head with a slight beak (metaballs) and a daisy on the crown.

Each head gets a thin face patch: the front of the skin lifted 2 mm and mapped into one
cell of the face atlas (2 x 2 cells, one per expression), so the shader picks the
expression by a UV offset (see ChibiFace.cs).

Bakes, per head, into <out>/<name>_*.npy (1024 px, float): part id, object-space
position, normal, ambient occlusion and curvature (pointiness). paint.py paints the
albedo from them. Exports <out>/<name>_head.glb and <out>/<name>_face.glb.
--preview only writes quick workbench renders of the shapes (<out>/preview_*.png).
"""
import math
import os
import sys

import bpy
import bmesh
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
out = os.path.abspath(argv[0] if argv else os.getcwd())
PREVIEW = "--preview" in argv
os.makedirs(out, exist_ok=True)
SIZE = 1024

# The rig's head (ChibiFigure.Build): hh = 1.35 / 2.8, chin = 1.35 - hh, centre half a
# head above the chin, 1 cm forward. Blender is Z up with -Y forward (Godot (x, y, z) is
# Blender (x, -z, y)).
HH = 1.35 / 2.8
CHIN = 1.35 - HH
C = Vector((0.0, -0.01, CHIN + HH * 0.5))
# The face patch's frame, relative to the head centre: x across, z up (metres). The
# face atlas paints in these coordinates (paint.py uses the same numbers).
FACE_X = (-0.19, 0.19)
FACE_Z = (-0.15, 0.11)

# Part ids, baked into the id map for paint.py.
SKIN, HAIR, PETAL, HEART, LEAF = 1, 2, 3, 4, 5
# Vertex colour: red marks skin, green hair; the shader tints those to the player's look.
TINT = {SKIN: (1, 0, 0, 1), HAIR: (0, 1, 0, 1), PETAL: (0, 0, 0, 1), HEART: (0, 0, 0, 1), LEAF: (0, 0, 0, 1)}

K = 1 / 0.575  # a metaball's surface sits at 0.575 of its radius (stiffness 2, threshold 0.6)


def link(ob):
    bpy.context.scene.collection.objects.link(ob)
    return ob


def meta(name, balls, resolution=0.006):
    """balls: (centre relative to C, surface radii (x, y, z), quaternion or None, sign)."""
    mb = bpy.data.metaballs.new(name)
    mb.resolution = mb.render_resolution = resolution
    mb.threshold = 0.6
    for c, r, q, sign in balls:
        e = mb.elements.new(type="ELLIPSOID")
        e.co = C + Vector(c)
        e.radius = K
        e.size_x, e.size_y, e.size_z = r
        e.stiffness = 2.0
        e.use_negative = sign < 0
        if q is not None:
            e.rotation = q
    ob = link(bpy.data.objects.new(name + "_mb", mb))
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob)
    return link(bpy.data.objects.new(name, me))


def ball(c, r, sign=1):
    return (c, (r, r, r) if np.isscalar(r) else r, None, sign)


def chain(p0, p1, ctrl, r0, r1, n=7, squash=1.0):
    """A lock of hair: balls along a quadratic curve from p0 (root) to p1 (tip), thinning."""
    p0, p1, ctrl = (np.array(v, float) for v in (p0, p1, ctrl))
    balls = []
    for i in range(n):
        t = i / (n - 1)
        p = (1 - t) ** 2 * p0 + 2 * (1 - t) * t * ctrl + t * t * p1
        r = r0 + (r1 - r0) * t ** 0.8
        balls.append(ball(tuple(p), (r, r, r * squash)))
    return balls


def on_head(az, z, out_by, rx=0.25, ry=0.236, rz=0.236):
    """A point on (or out_by metres off) the head ellipsoid at azimuth az (degrees, 0 is
    the front, +90 the figure's left, +X) and height z above the centre."""
    a = math.radians(az)
    s = math.sqrt(max(0.0, 1 - (z / rz) ** 2))
    return (math.sin(a) * (rx * s + out_by), -math.cos(a) * (ry * s + out_by), z)


def smooth_mesh(ob, iterations=4, ratio=None):
    bpy.context.view_layer.objects.active = ob
    if ratio:
        m = ob.modifiers.new("dec", "DECIMATE")
        m.ratio = ratio
        bpy.ops.object.modifier_apply(modifier=m.name)
    m = ob.modifiers.new("smooth", "CORRECTIVE_SMOOTH")
    m.iterations = iterations
    m.smooth_type = "LENGTH_WEIGHTED"
    m.use_only_smooth = True
    bpy.ops.object.modifier_apply(modifier=m.name)


def set_part(ob, part):
    ob["part"] = part


def bramble():
    # The skin head: round, a touch wider than tall, the cheeks a little full.
    bpy.ops.mesh.primitive_uv_sphere_add(segments=40, ring_count=26, radius=1.0)
    skin = bpy.context.active_object
    skin.name = "BrambleSkin"
    for v in skin.data.vertices:
        x, y, z = v.co
        cheek = math.exp(-((z + 0.3) / 0.3) ** 2) * max(0.0, -y)
        v.co = Vector((x * 0.25 * (1 + 0.07 * cheek), y * 0.232 * (1 - 0.03 * max(0.0, -y)), z * 0.236 * (1 - 0.04 * max(0.0, -z) ** 2))) + C
    bpy.ops.object.shade_smooth()
    set_part(skin, SKIN)

    balls = [ball((0, 0.035, 0.05), (0.262, 0.262, 0.218))]
    # Fringe: chunky locks from the crown down over the forehead, swept to the
    # figure's right (-X), the middle ones lowest.
    for az in (-66, -44, -22, 0, 22, 44, 66):
        lift = abs(az) / 66
        root = on_head(az + 6, 0.17, 0.0)
        tip = on_head(az - 16, 0.05 + 0.035 * lift, 0.026)
        ctrl = on_head(az - 4, 0.13, 0.05)
        balls += chain(root, tip, ctrl, 0.052, 0.02, 11)
    # Sides: locks down past the ears that flick outward at the ends.
    for side in (-1, 1):
        for az, low in ((84, -0.035), (104, -0.06), (124, -0.08)):
            root = on_head(side * az, 0.12, 0.0)
            tip = on_head(side * (az + 6), low, 0.045)
            ctrl = on_head(side * az, 0.0, 0.025)
            balls += chain(root, tip, ctrl, 0.056, 0.021, 11)
    # The back: long locks to the nape, their ends turning out a little.
    for az in (146, 168, 192, 214):
        root = on_head(az, 0.1, 0.0)
        tip = on_head(az, -0.17, 0.05)
        ctrl = on_head(az, -0.06, 0.03)
        balls += chain(root, tip, ctrl, 0.064, 0.024, 12)
    # The crown tuft: a fat lock standing up and curling back, like the turnaround's.
    balls += chain((0.0, 0.0, 0.21), (0.03, 0.075, 0.325), (0.01, -0.035, 0.345), 0.05, 0.015, 14)
    hair = meta("BrambleHair", balls)
    smooth_mesh(hair, 6, ratio=0.1)
    bpy.context.view_layer.objects.active = hair
    bpy.ops.object.shade_smooth()
    set_part(hair, HAIR)
    return [skin, hair], skin


def pebble():
    # A smooth turtle head: round, with the snout pushed forward into a soft beak and
    # gentle brow ridges.
    balls = [
        ball((0, 0.0, 0.0), (0.25, 0.236, 0.236)),
        ball((0, -0.14, -0.075), (0.15, 0.125, 0.1)),
        ball((0, -0.205, -0.108), (0.045, 0.035, 0.03)),
    ]
    for side in (-1, 1):
        balls.append(ball((side * 0.1, -0.16, 0.06), (0.06, 0.035, 0.028)))
    skin = meta("PebbleSkin", balls, resolution=0.008)
    smooth_mesh(skin, 4, ratio=0.12)
    bpy.context.view_layer.objects.active = skin
    bpy.ops.object.shade_smooth()
    set_part(skin, SKIN)
    # The top of the head, to seat the daisy on.
    bvh = BVHTree.FromObject(skin, bpy.context.evaluated_depsgraph_get())
    flower = C + Vector((0.035, 0.0, 0.0))
    hit = bvh.ray_cast(flower + Vector((0, 0, 1)), Vector((0, 0, -1)))[0]
    top = hit + Vector((0, 0, 0.006))
    parts = [skin]
    petals = []
    for i in range(9):
        a = i * math.tau / 9 + 0.2
        d = Vector((math.cos(a), math.sin(a), 0.0))
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=1.0)
        p = bpy.context.active_object
        p.scale = (0.056, 0.022, 0.008)
        p.rotation_euler = (0.0, -0.28, a)
        p.location = top + d * 0.056 + Vector((0, 0, 0.004))
        petals.append(p)
    for p in petals:
        bpy.context.view_layer.objects.active = p
        p.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.select_all(action="DESELECT")
    for p in petals:
        p.select_set(True)
    bpy.context.view_layer.objects.active = petals[0]
    bpy.ops.object.join()
    petal = bpy.context.active_object
    petal.name = "PebblePetals"
    bpy.ops.object.shade_smooth()
    set_part(petal, PETAL)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, radius=1.0, location=top + Vector((0, 0, 0.009)))
    heart = bpy.context.active_object
    heart.scale = (0.029, 0.029, 0.015)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.shade_smooth()
    heart.name = "PebbleHeart"
    set_part(heart, HEART)
    # One small leaf tucked under the flower.
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=1.0)
    leaf = bpy.context.active_object
    leaf.scale = (0.03, 0.013, 0.005)
    leaf.rotation_euler = (0.25, -0.15, -2.2)
    leaf.location = top + Vector((-0.03, 0.03, -0.004))
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.shade_smooth()
    set_part(leaf, LEAF)
    bpy.ops.object.select_all(action="DESELECT")
    return parts + [petal, heart, leaf], skin


def face_patch(name, skin):
    """The thin face: the front of the skin, 2 mm out, its UVs in the first atlas cell."""
    bvh = BVHTree.FromObject(skin, bpy.context.evaluated_depsgraph_get())
    nx, nz = 32, 24
    grid, keep = {}, {}
    for j in range(nz + 1):
        for i in range(nx + 1):
            x = FACE_X[0] + (FACE_X[1] - FACE_X[0]) * i / nx
            z = FACE_Z[1] - (FACE_Z[1] - FACE_Z[0]) * j / nz
            hit, normal, _, _ = bvh.ray_cast(C + Vector((x, -1.0, z)), Vector((0, 1, 0)))
            if hit is None or normal.y > -0.2:
                continue
            grid[i, j] = hit + normal * 0.002
            u = (x - FACE_X[0]) / (FACE_X[1] - FACE_X[0]) * 0.5
            vg = (FACE_Z[1] - z) / (FACE_Z[1] - FACE_Z[0]) * 0.5
            keep[i, j] = (u, 1 - vg)  # Blender's v runs up; glTF flips it back
    bm = bmesh.new()
    verts = {k: bm.verts.new(p) for k, p in grid.items()}
    uv_layer = bm.loops.layers.uv.new("UVMap")
    for j in range(nz):
        for i in range(nx):
            q = [(i, j), (i, j + 1), (i + 1, j + 1), (i + 1, j)]
            if all(k in verts for k in q):
                f = bm.faces.new([verts[k] for k in q])
                for loop, k in zip(f.loops, q):
                    loop[uv_layer].uv = keep[k]
    bm.normal_update()
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    for f in me.polygons:
        f.use_smooth = True
    ob = link(bpy.data.objects.new(name, me))
    # Face outward (towards -Y).
    if np.mean([p.normal.y for p in me.polygons]) > 0:
        me.flip_normals()
    return ob


def join(name, parts):
    """One mesh from the parts, with a material per part id and vertex colour tints."""
    mats = {}
    for p in parts:
        part = p["part"]
        if part not in mats:
            mats[part] = bpy.data.materials.new(f"{name}_part{part}")
            mats[part]["part"] = part
        p.data.materials.clear()
        p.data.materials.append(mats[part])
        p.data.color_attributes.new("Color", "FLOAT_COLOR", "CORNER")
        col = p.data.color_attributes["Color"]
        col.data.foreach_set("color", np.tile(TINT[part], len(col.data)))
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    ob = bpy.context.active_object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return ob


def unwrap(ob):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    if not ob.data.uv_layers:
        ob.data.uv_layers.new(name="UVMap")
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.006, area_weight=1.0, scale_to_bounds=True)
    bpy.ops.object.mode_set(mode="OBJECT")


def occluder():
    """A stand-in torso under the chin, so the AO darkens where head meets body."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(0, 0, CHIN - 0.15))
    body = bpy.context.active_object
    body.scale = (0.2, 0.17, 0.2)
    body.name = "Occluder"
    return body


def emit_tree(mat, which):
    """Point the material at one bake pass: an emission of the id, position, normal or
    curvature, each encoded into 0..1."""
    nt = mat.node_tree
    for n in list(nt.nodes):
        if n.bl_idname != "ShaderNodeTexImage":
            nt.nodes.remove(n)
    outp = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(em.outputs[0], outp.inputs[0])
    if which == "id":
        em.inputs[0].default_value = (mat["part"] / 10, 0, 0, 1)
        return
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    if which == "curv":
        nt.links.new(geo.outputs["Pointiness"], em.inputs[0])
        return
    src = geo.outputs["Position"] if which == "pos" else geo.outputs["Normal"]
    scale, offset = (0.25, 0.5) if which == "pos" else (0.5, 0.5)
    mul = nt.nodes.new("ShaderNodeVectorMath")
    mul.operation = "MULTIPLY_ADD"
    mul.inputs[1].default_value = (scale,) * 3
    mul.inputs[2].default_value = (offset,) * 3
    nt.links.new(src, mul.inputs[0])
    nt.links.new(mul.outputs[0], em.inputs[0])


def bake(name, ob):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.render.bake.use_selected_to_active = False
    scene.render.bake.margin = 8
    img = bpy.data.images.new(name + "_bake", SIZE, SIZE, alpha=True, float_buffer=True)
    img.colorspace_settings.name = "Non-Color"
    for mat in ob.data.materials:
        mat.use_nodes = True
        tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
        tex.image = img
        mat.node_tree.nodes.active = tex
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    for which in ("id", "pos", "nrm", "curv", "ao"):
        img.pixels.foreach_set(np.zeros(SIZE * SIZE * 4, np.float32))
        if which == "ao":
            scene.cycles.samples = 96
            bpy.ops.object.bake(type="AO")
        else:
            scene.cycles.samples = 1
            for mat in ob.data.materials:
                emit_tree(mat, which)
            bpy.ops.object.bake(type="EMIT")
        px = np.empty(SIZE * SIZE * 4, np.float32)
        img.pixels.foreach_get(px)
        # Blender's rows run bottom up; flip so row 0 is the top, as in a PNG.
        np.save(os.path.join(out, f"{name}_{which}.npy"), px.reshape(SIZE, SIZE, 4)[::-1])
        print("baked", name, which)
    for mat in ob.data.materials:
        mat.node_tree.nodes.clear()


def export(ob, path):
    ob.data.materials.clear()
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.gltf(
        filepath=path, use_selection=True, export_format="GLB", export_texcoords=True,
        export_normals=True, export_vertex_color="ACTIVE", export_materials="NONE", export_yup=True,
    )
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    print(f"exported {os.path.basename(path)}: {len(ob.data.vertices)} verts, {tris} tris")


def preview(objs, tag):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "OBJECT"
    scene.render.resolution_x = scene.render.resolution_y = 512
    cam = bpy.data.objects.get("PreviewCam") or link(bpy.data.objects.new("PreviewCam", bpy.data.cameras.new("PreviewCam")))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 0.75
    scene.camera = cam
    for o in bpy.data.objects:
        o.hide_render = o not in objs
    for yaw in (0, 35, 90, 180):
        a = math.radians(yaw)
        cam.location = C + Vector((math.sin(a) * 2, -math.cos(a) * 2, 0.05))
        cam.rotation_euler = (math.radians(90), 0, a)
        scene.render.filepath = os.path.join(out, f"preview_{tag}_{yaw}.png")
        bpy.ops.render.render(write_still=True)


bpy.ops.wm.read_factory_settings(use_empty=True)
body = occluder()
for name, builder in (("bramble", bramble), ("pebble", pebble)):
    parts, skin = builder()
    face = face_patch(name.capitalize() + "Face", skin)
    for p, col in zip(parts, ((0.95, 0.79, 0.63, 1), (0.6, 0.36, 0.22, 1), (0.97, 0.95, 0.9, 1), (0.95, 0.8, 0.35, 1))):
        p.color = col
    face.color = (0.2, 0.2, 0.25, 1)
    head = join(name.capitalize() + "Head", parts)
    if PREVIEW:
        preview([head, face], name)
        head.hide_render = face.hide_render = True
        continue
    unwrap(head)
    # The face patch sits 2 mm off the skin; keep it out of the AO.
    face.hide_render = True
    bake(name, head)
    export(head, os.path.join(out, f"{name}_head.glb"))
    export(face, os.path.join(out, f"{name}_face.glb"))
    for o in (head, face):
        o.hide_render = True
        o.hide_set(True)
    # The next head bakes without this one in the way.
    bpy.data.objects.remove(head)
    bpy.data.objects.remove(face)
