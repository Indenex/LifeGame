extends Node
# Unity spike only (never committed into client/). Loaded as an autoload in a
# render checkout through override.cfg (see export_scene.sh). After
# --spike-export-delay seconds of real time it writes into --spike-export-dir:
#   scene.json      camera, sun, environment (sky, fog, ambient, tonemap, glow,
#                   SSAO, adjustments), the player, and every draw:
#                   {mesh, material, shadow, count, instance data offset}
#   materials.json  every material: its shader file and parameters
#   shaders/        the source of every shader used (the look spec)
#   textures/       every texture a material uses, as PNG
#   meshes/         every mesh, one .bin each (format in unity/Assets/LifeGame/Runtime/SpikeData.cs)
#   godot.png       the frame itself, HUD hidden
#   instances.bin   per draw: count x (12 floats transform, 4 colour, 4 custom)
# Only visible 3D geometry within --spike-export-radius (default 140 m) of the
# player is written. Without the flags it does nothing.

var out_dir := ""
var delay := 0.0
var radius := 140.0
var t0 := 0

var mesh_ids := {}      # Mesh -> id
var mat_ids := {}       # Material -> id
var shader_ids := {}    # Shader -> file name
var tex_ids := {}       # Texture2D -> file name
var materials := []
var draws := []
var inst: FileAccess
var inst_floats := 0

func _ready() -> void:
	for a in OS.get_cmdline_user_args() + OS.get_cmdline_args():
		if a.begins_with("--spike-export-dir="):
			out_dir = a.substr(19)
		if a.begins_with("--spike-export-delay="):
			delay = float(a.substr(21))
		if a.begins_with("--spike-export-radius="):
			radius = float(a.substr(22))
	set_process(out_dir != "")
	t0 = Time.get_ticks_msec()

func _process(_d: float) -> void:
	if Time.get_ticks_msec() - t0 < delay * 1000.0:
		return
	set_process(false)
	for d in ["shaders", "textures", "meshes"]:
		DirAccess.make_dir_recursive_absolute(out_dir + "/" + d)
	var root := get_tree().root
	var info := {}

	# The game camera is CameraRig's (its world is drawn in a SubViewport, Main.cs).
	var cam: Camera3D = null
	for c in root.find_children("*", "Camera3D", true, false):
		var p = c.get_parent()
		if p != null and p.get_script() != null and str(p.get_script().resource_path).ends_with("CameraRig.cs"):
			cam = c.get_viewport().get_camera_3d()  # render_cam.gd's own camera when it is in use
	if cam:
		info["camera"] = {"origin": v3(cam.global_position), "forward": v3(-cam.global_basis.z),
			"up": v3(cam.global_basis.y), "fov": cam.fov, "near": cam.near, "far": cam.far}
	var player := Vector3.ZERO
	for n in root.find_children("*", "Node3D", true, false):
		if n.get_script() != null and str(n.get_script().resource_path).ends_with("/Avatar.cs"):
			player = n.global_position
			info["player"] = {"origin": v3(player)}
			break

	# Lights and environment of the camera's own world only (UI previews have their own).
	var world: World3D = cam.get_world_3d() if cam else get_viewport().world_3d
	info["suns"] = root.find_children("*", "DirectionalLight3D", true, false).filter(
		func(s): return s.is_visible_in_tree() and s.get_world_3d() == world).map(
		func(s): return props(s).merged({"forward": v3(-s.global_basis.z)}))
	info["omnis"] = root.find_children("*", "OmniLight3D", true, false).filter(
		func(s): return s.is_visible_in_tree() and s.get_world_3d() == world and s.global_position.distance_to(player) < radius).map(
		func(s): return {"origin": v3(s.global_position), "color": conv(s.light_color), "energy": s.light_energy, "range": s.omni_range, "attenuation": s.omni_attenuation, "shadow": s.shadow_enabled})
	var e: Environment = world.environment if world.environment else world.fallback_environment
	if cam and cam.environment:
		e = cam.environment
	if e:
		info["environment"] = props(e)
		if e.adjustment_color_correction:
			info["environment"]["adjustment_color_correction"] = tex(e.adjustment_color_correction)
		if e.sky and e.sky.sky_material:
			info["sky_material"] = mat_id(e.sky.sky_material)
	if cam and cam.attributes:
		info["camera_attributes"] = props(cam.attributes)
	elif world.camera_attributes:
		info["camera_attributes"] = props(world.camera_attributes)
	info["global_shader_params"] = {}
	for g in ["wetness", "wind_strength", "wind_gust", "wind_dir", "rain_amount", "cloud_cover", "cloud_offset",
			"world_seconds", "far_land_radius", "fog_tint", "season_warmth", "season_autumn", "snow_cover",
			"snowing", "season_year", "puddles", "player_position", "view_camera_position"]:
		info["global_shader_params"][g] = conv(RenderingServer.global_shader_parameter_get(g))

	inst = FileAccess.open(out_dir + "/instances.bin", FileAccess.WRITE)
	for n in root.find_children("*", "GeometryInstance3D", true, false):
		if n.get_world_3d() != world:
			continue
		if not n.is_visible_in_tree() or n.global_position.distance_to(player) > radius * 1.5:
			continue
		if n is MeshInstance3D and n.mesh:
			add_mesh_instance(n, player)
		elif n is MultiMeshInstance3D and n.multimesh and n.multimesh.mesh:
			add_multimesh(n, player)
	inst.close()
	info["draws"] = draws
	write_json("scene.json", info)
	write_json("materials.json", materials)
	# The same frame as a picture, without the HUD (the world's own SubViewport),
	# for the side-by-side.
	for c in root.find_children("*", "Label3D", true, false):
		c.visible = false
	await get_tree().process_frame
	await get_tree().process_frame
	await RenderingServer.frame_post_draw
	var vp: Viewport = cam.get_viewport() if cam else get_viewport()
	vp.get_texture().get_image().save_png(out_dir + "/godot.png")
	print("[spike-export] %d draws, %d meshes, %d materials, %d shaders, %d textures" % [draws.size(), mesh_ids.size(), mat_ids.size(), shader_ids.size(), tex_ids.size()])
	get_tree().quit()

func add_mesh_instance(n: MeshInstance3D, player: Vector3) -> void:
	if n.global_position.distance_to(player) > radius:
		return
	var mesh: Mesh = n.mesh
	if n.skin != null or n.get_parent() is Skeleton3D:
		var baked = n.bake_mesh_from_current_skeleton_pose()
		if baked: mesh = baked
	var d := draw_base(n, mesh, n.mesh)
	d["count"] = 1
	d["offset"] = inst_floats
	write_inst(n.global_transform, Color(1, 1, 1, 1), Color(0, 0, 0, 0))
	d["name"] = str(n.get_path())
	draws.append(d)

func add_multimesh(n: MultiMeshInstance3D, player: Vector3) -> void:
	var mm := n.multimesh
	var d := draw_base(n, mm.mesh, mm.mesh)
	d["offset"] = inst_floats
	d["name"] = str(n.get_path())
	var count := 0
	var xf := n.global_transform
	var vis := mm.visible_instance_count if mm.visible_instance_count >= 0 else mm.instance_count
	for i in vis:
		var t := xf * mm.get_instance_transform(i)
		if Vector2(t.origin.x - player.x, t.origin.z - player.z).length() > radius:
			continue
		var c := mm.get_instance_color(i) if mm.use_colors else Color(1, 1, 1, 1)
		var cu := mm.get_instance_custom_data(i) if mm.use_custom_data else Color(0, 0, 0, 0)
		write_inst(t, c, cu)
		count += 1
	d["count"] = count
	d["multimesh"] = true
	if count > 0:
		draws.append(d)

# `mesh` is what gets drawn (a skinned mesh baked into its current pose); materials
# come from `source`, the node's own mesh, since a baked copy carries none.
func draw_base(n: GeometryInstance3D, mesh: Mesh, source: Mesh) -> Dictionary:
	var d := {"mesh": mesh_id(mesh), "shadow": n.cast_shadow,
		"visibility_end": n.visibility_range_end, "visibility_begin": n.visibility_range_begin}
	if n.material_override:
		d["material"] = mat_id(n.material_override)
	var surf := []
	for i in mesh.get_surface_count():
		var m = null
		if n is MeshInstance3D and i < n.get_surface_override_material_count() and n.get_surface_override_material(i):
			m = n.get_surface_override_material(i)
		elif i < source.get_surface_count() and source.surface_get_material(i):
			m = source.surface_get_material(i)
		surf.append(mat_id(m) if m else -1)
	d["surface_materials"] = surf
	return d

func write_inst(t: Transform3D, c: Color, cu: Color) -> void:
	for v in [t.basis.x, t.basis.y, t.basis.z, t.origin]:
		inst.store_float(v.x); inst.store_float(v.y); inst.store_float(v.z)
	for v in [c.r, c.g, c.b, c.a, cu.r, cu.g, cu.b, cu.a]:
		inst.store_float(v)
	inst_floats += 20

func mesh_id(m: Mesh) -> int:
	if mesh_ids.has(m):
		return mesh_ids[m]
	var id := mesh_ids.size()
	mesh_ids[m] = id
	if not (m is ArrayMesh):
		# PrimitiveMesh (the water plane, spheres...): bake it into an ArrayMesh.
		var am := ArrayMesh.new()
		for s in m.get_surface_count():
			am.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, m.surface_get_arrays(s))
			if m.surface_get_material(s): am.surface_set_material(s, m.surface_get_material(s))
		m = am
	var f := FileAccess.open("%s/meshes/mesh_%04d.bin" % [out_dir, id], FileAccess.WRITE)
	f.store_32(0x4853454D)  # "MESH"
	f.store_32(m.get_surface_count())
	for s in m.get_surface_count():
		var a: Array = m.surface_get_arrays(s)
		var fmt: int = m.surface_get_format(s)
		var verts: PackedVector3Array = a[Mesh.ARRAY_VERTEX]
		var nv := verts.size()
		var idx = a[Mesh.ARRAY_INDEX]
		var ni: int = idx.size() if idx != null else 0
		f.store_32(m.surface_get_primitive_type(s))
		f.store_32(nv)
		f.store_32(ni)
		# flags: 1 normal, 2 color, 4 uv, 8 uv2, 16 custom0
		var flags := 0
		if a[Mesh.ARRAY_NORMAL] != null: flags |= 1
		if a[Mesh.ARRAY_COLOR] != null: flags |= 2
		if a[Mesh.ARRAY_TEX_UV] != null: flags |= 4
		if a[Mesh.ARRAY_TEX_UV2] != null: flags |= 8
		if a[Mesh.ARRAY_CUSTOM0] != null: flags |= 16
		f.store_32(flags)
		for v in verts:
			f.store_float(v.x); f.store_float(v.y); f.store_float(v.z)
		if flags & 1:
			for v in a[Mesh.ARRAY_NORMAL]:
				f.store_float(v.x); f.store_float(v.y); f.store_float(v.z)
		if flags & 2:
			for c in a[Mesh.ARRAY_COLOR]:
				f.store_float(c.r); f.store_float(c.g); f.store_float(c.b); f.store_float(c.a)
		if flags & 4:
			for v in a[Mesh.ARRAY_TEX_UV]:
				f.store_float(v.x); f.store_float(v.y)
		if flags & 8:
			for v in a[Mesh.ARRAY_TEX_UV2]:
				f.store_float(v.x); f.store_float(v.y)
		if flags & 16:
			var cf: int = (fmt >> Mesh.ARRAY_FORMAT_CUSTOM0_SHIFT) & Mesh.ARRAY_FORMAT_CUSTOM_MASK
			var raw = a[Mesh.ARRAY_CUSTOM0]
			var floats := PackedFloat32Array()
			if cf == Mesh.ARRAY_CUSTOM_RGBA_FLOAT:
				floats = raw
			elif cf == Mesh.ARRAY_CUSTOM_RGBA8_UNORM:
				for b in raw: floats.append(b / 255.0)
			else:
				floats.resize(nv * 4)
				push_warning("custom0 format %d not exported" % cf)
			for k in nv * 4:
				f.store_float(floats[k] if k < floats.size() else 0.0)
		if ni > 0:
			for i in idx: f.store_32(i)
	f.close()
	return id

func mat_id(m: Material) -> int:
	if mat_ids.has(m):
		return mat_ids[m]
	var id := mat_ids.size()
	mat_ids[m] = id
	var d := {"id": id, "class": m.get_class()}
	if m is ShaderMaterial and m.shader:
		if not shader_ids.has(m.shader):
			var fn := "shader_%02d.gdshader" % shader_ids.size()
			shader_ids[m.shader] = fn
			var f := FileAccess.open(out_dir + "/shaders/" + fn, FileAccess.WRITE)
			f.store_string(m.shader.code)
			f.close()
		d["shader"] = shader_ids[m.shader]
		var params := {}
		for u in m.shader.get_shader_uniform_list():
			var v = m.get_shader_parameter(u.name)
			params[u.name] = tex(v) if v is Texture2D else conv(v)
		d["params"] = params
	else:
		var p := props(m)
		for k in p.keys():
			var v = m.get(k)
			if v is Texture2D:
				p[k] = tex(v)
		d["params"] = p
	materials.append(d)
	return id

func tex(t: Texture2D):
	if t == null:
		return null
	if tex_ids.has(t):
		return tex_ids[t]
	var fn := "tex_%02d.png" % tex_ids.size()
	tex_ids[t] = fn
	if t is NoiseTexture2D:
		await_noise(t)
	var img := t.get_image()
	if img:
		if img.is_compressed(): img.decompress()
		img.save_png(out_dir + "/textures/" + fn)
	return fn

func await_noise(_t: NoiseTexture2D) -> void:
	pass  # noise textures are generated long before the export runs

func write_json(name: String, v) -> void:
	var f := FileAccess.open(out_dir + "/" + name, FileAccess.WRITE)
	f.store_string(JSON.stringify(v, " ", false))
	f.close()

func v3(v: Vector3) -> Array:
	return [v.x, v.y, v.z]

func conv(v):
	if v is Color: return [v.r, v.g, v.b, v.a]
	if v is Vector3: return v3(v)
	if v is Vector2: return [v.x, v.y]
	if v is Vector4: return [v.x, v.y, v.z, v.w]
	if v is Object: return v.get_class() if v != null else null
	if v is PackedFloat32Array or v is PackedColorArray: return Array(v).map(conv)
	return v

func props(o: Object) -> Dictionary:
	var d := {}
	for p in o.get_property_list():
		if not (p.usage & PROPERTY_USAGE_STORAGE) or p.type in [TYPE_CALLABLE, TYPE_SIGNAL, TYPE_PACKED_BYTE_ARRAY, TYPE_PACKED_VECTOR3_ARRAY]:
			continue
		var v = o.get(p.name)
		d[p.name] = (v.get_class() if v else null) if p.type == TYPE_OBJECT else conv(v)
	return d
