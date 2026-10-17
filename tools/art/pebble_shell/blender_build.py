"""Pebble's shell, step 1 of 2 (art-rnd-1): run inside Blender.

    blender -b --factory-startup -P tools/art/pebble_shell/blender_build.py -- <out dir>

Builds the game mesh (smooth carapace, its wall and the plastron, about 2.4k
triangles) and a carved high mesh (grooved seams, pillowed scutes), bakes the high
mesh's ambient occlusion and normals onto the game mesh's texture with Cycles on the
CPU, and exports the game mesh as glTF. paint.py then paints the albedo over the AO.

Writes <out>/pebble_shell.glb, <out>/bake_ao.png, <out>/shell_normal.png.
"""
import os
import sys

import bpy
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import geom  # noqa: E402

out = os.path.abspath(sys.argv[sys.argv.index("--") + 1]) if "--" in sys.argv else os.getcwd()
os.makedirs(out, exist_ok=True)
SIZE = 1024


def to_blender(v):
    # Godot (x, y up, z forward) -> Blender (x, -z, y up); the glTF exporter turns it back.
    v = np.asarray(v)
    return np.stack([v[:, 0], -v[:, 2], v[:, 1]], -1)


def mesh_object(name, parts):
    """parts: list of (verts, faces, uvs, tint) in Godot space; tint 1 means the look's
    shell colour applies (vertex colour red), 0 means it doesn't (the plastron)."""
    verts, faces, uvs, tints, off = [], [], [], [], 0
    for v, f, uv, tint in parts:
        verts.append(to_blender(v))
        faces += [tuple(i + off for i in face) for face in f]
        uvs.append(np.asarray(uv))
        tints.append(np.full(len(v), tint, float))
        off += len(v)
    verts, uvs, tints = np.concatenate(verts), np.concatenate(uvs), np.concatenate(tints)
    # Wind every face so its normal points away from the body, except the shell's inside,
    # which faces the body.
    centre = np.array([0.0, 0.0, 0.56])
    fixed = []
    for face in faces:
        p = verts[list(face)]
        n = np.cross(p[1] - p[0], p[2] - p[0])
        inside = np.allclose(uvs[list(face)], geom.INNER_UV)
        want = -1 if inside else 1
        fixed.append(face if np.dot(n, p.mean(0) - centre) * want >= 0 else tuple(reversed(face)))
    faces = fixed
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts.tolist(), [], faces)
    me.update()
    me.uv_layers.new(name="UVMap")
    me.color_attributes.new("Color", "FLOAT_COLOR", "CORNER")
    # Per-corner data, set in one go once both layers exist (adding a layer
    # invalidates references to the others).
    loop_vi = np.zeros(len(me.loops), int)
    me.loops.foreach_get("vertex_index", loop_vi)
    me.uv_layers["UVMap"].data.foreach_set("uv", uvs[loop_vi].ravel())
    t = tints[loop_vi]
    me.color_attributes["Color"].data.foreach_set("color", np.stack([t, t, t, np.ones_like(t)], -1).ravel())
    me.polygons.foreach_set("use_smooth", np.ones(len(me.polygons), bool))
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# The game mesh: the smooth dome carries clean light bands; the carving comes from the
# normal map and the painting.
low = mesh_object("PebbleShell", [
    (*geom.carapace(14, 72), 1.0),
    (*geom.wall(72), 1.0),
    (*geom.plastron(7, 40), 0.0),
])
# The carved high mesh, same UVs.
high = mesh_object("PebbleShellHigh", [
    (*geom.carapace(220, 900, relief=True), 1.0),
    (*geom.plastron(90, 360, relief=True), 0.0),
])
# A stand-in body, so the AO darkens where the shell meets the torso.
bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1.0, location=(0, 0, 0.56))
body = bpy.context.active_object
body.scale = (0.2, 0.17, 0.3)

# Bake targets.
images = {}
for name, colorspace in (("bake_ao", "sRGB"), ("shell_normal", "Non-Color")):
    img = bpy.data.images.new(name, SIZE, SIZE, alpha=False, float_buffer=False)
    img.colorspace_settings.name = colorspace
    images[name] = img
mat = bpy.data.materials.new("bake")
mat.use_nodes = True
tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
mat.node_tree.nodes.active = tex
low.data.materials.append(mat)
high.data.materials.append(bpy.data.materials.new("high"))

scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 64
scene.render.bake.use_selected_to_active = True
scene.render.bake.cage_extrusion = 0.03
scene.render.bake.max_ray_distance = 0.08
scene.render.bake.margin = 6

bpy.ops.object.select_all(action="DESELECT")
high.select_set(True)
low.select_set(True)
bpy.context.view_layer.objects.active = low
for name, kind in (("bake_ao", "AO"), ("shell_normal", "NORMAL")):
    tex.image = images[name]
    bpy.ops.object.bake(type=kind)
    images[name].filepath_raw = os.path.join(out, name + ".png")
    images[name].file_format = "PNG"
    images[name].save()
    print("baked", name)

# Export only the game mesh, without its bake material.
low.data.materials.clear()
bpy.ops.object.select_all(action="DESELECT")
low.select_set(True)
bpy.context.view_layer.objects.active = low
bpy.ops.export_scene.gltf(
    filepath=os.path.join(out, "pebble_shell.glb"),
    use_selection=True,
    export_format="GLB",
    export_texcoords=True,
    export_normals=True,
    export_tangents=True,
    export_vertex_color="ACTIVE",
    export_materials="NONE",
    export_yup=True,
)
tris = sum(len(p.vertices) - 2 for p in low.data.polygons)
print(f"exported pebble_shell.glb: {len(low.data.vertices)} verts, {tris} tris")
