extends Node
# Render lane only (never committed into client/): lib.sh copies this into the
# render checkout and loads it as an autoload through override.cfg.
#
# --render-cam=distance,height,yaw[,look_height] puts a camera of its own around
# the player: `distance` metres away on the ground, `height` metres up, `yaw`
# degrees around from the game camera's usual spot behind (0 = same side as the
# game camera, 180 = the far side), looking at `look_height` metres above the
# player's feet (default 1). A fifth value orbits the camera that many degrees per
# second of game time (--render-cam=5,2.5,0,1,30 goes round once in 12 s).
# --render-cam-target=sheep frames the sheep nearest the player instead (yaw then
# counts from the game camera's direction too).
# --render-slowstart=scale,seconds runs game time at `scale` for the first
# `seconds` of real time, so things a lane times from the start (a mock window
# opening 1.5 s in) happen after the world has loaded. --screenshot-delay counts
# game time, so it stretches too (delay 3 at 0.05 = 60 real seconds).
# Without these flags it does nothing.

var cfg: PackedFloat32Array
var rig: Node3D
var cam: Camera3D
var target_kind := ""
var slow_until_ms := -1
var orbit := 0.0

func _ready() -> void:
	for a in OS.get_cmdline_user_args() + OS.get_cmdline_args():
		if a.begins_with("--render-cam="):
			cfg = PackedFloat32Array(Array(a.substr(13).split(",")).map(func(s): return float(s)))
		if a.begins_with("--render-cam-target="):
			target_kind = a.substr(20)
		if a.begins_with("--render-slowstart="):
			var p = a.substr(19).split(",")
			Engine.time_scale = float(p[0])
			slow_until_ms = Time.get_ticks_msec() + int(float(p[1]) * 1000.0)
	if cfg.size() >= 3 and cfg.size() < 4:
		cfg.append(1.0)
	set_process(cfg.size() >= 3 or slow_until_ms >= 0)

func _process(_delta: float) -> void:
	if slow_until_ms >= 0 and Time.get_ticks_msec() >= slow_until_ms:
		Engine.time_scale = 1.0
		slow_until_ms = -1
	if cfg.size() < 3:
		return
	if rig == null:
		for c in get_tree().root.find_children("*", "Camera3D", true, false):
			var p = c.get_parent()
			if p is Node3D and p.get_script() != null and str(p.get_script().resource_path).ends_with("CameraRig.cs"):
				rig = p
				cam = Camera3D.new()
				cam.fov = 40.0
				cam.near = 0.1
				cam.far = 250.0
				cam.attributes = c.attributes
				p.get_parent().add_child(cam)
				break
		if rig == null:
			return
	cam.make_current()
	var feet := rig.global_position - Vector3(0, 1.2, 0)
	if target_kind == "sheep":
		var best: Node3D = null
		for n in get_tree().root.find_children("*", "Node3D", true, false):
			if n.get_script() != null and str(n.get_script().resource_path).ends_with("/Sheep.cs") and n.is_visible_in_tree():
				if best == null or n.global_position.distance_to(feet) < best.global_position.distance_to(feet):
					best = n
		if best != null:
			feet = best.global_position
	if cfg.size() > 4:
		orbit += cfg[4] * _delta
	var yaw := rig.global_rotation.y + deg_to_rad(cfg[2] + orbit)
	cam.global_position = feet + Vector3(sin(yaw), 0, cos(yaw)) * cfg[0] + Vector3.UP * cfg[1]
	cam.look_at(feet + Vector3.UP * cfg[3])
