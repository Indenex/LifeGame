extends Node
# Render lane only (never committed into client/): lib.sh copies this into the
# render checkout and loads it as an autoload through override.cfg.
#
# --render-cam=distance,height,yaw[,look_height] puts a camera of its own around
# the player: `distance` metres away on the ground, `height` metres up, `yaw`
# degrees around from the game camera's usual spot behind (0 = same side as the
# game camera, 180 = the far side), looking at `look_height` metres above the
# player's feet (default 1). Without the flag it does nothing.

var cfg: PackedFloat32Array
var rig: Node3D
var cam: Camera3D

func _ready() -> void:
	for a in OS.get_cmdline_user_args() + OS.get_cmdline_args():
		if a.begins_with("--render-cam="):
			cfg = PackedFloat32Array(Array(a.substr(13).split(",")).map(func(s): return float(s)))
	if cfg.size() < 3:
		set_process(false)
		return
	if cfg.size() < 4:
		cfg.append(1.0)

func _process(_delta: float) -> void:
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
	var yaw := rig.global_rotation.y + deg_to_rad(cfg[2])
	cam.global_position = feet + Vector3(sin(yaw), 0, cos(yaw)) * cfg[0] + Vector3.UP * cfg[1]
	cam.look_at(feet + Vector3.UP * cfg[3])
