extends Node
# Key test (bugfix-41): runs inside the real client (loaded as an autoload by run.sh, never
# committed into client/). After the world has loaded it presses I and checks the bag opened,
# then presses I again and checks it closed. Exits 0 on PASS, 1 on FAIL.
#   Justin: "the inventory won't open" - a panel counting as open (the look picker opens by
#   itself at first join) silently swallowed the key.
var t := 0.0
var step := 0

func find_by_script(suffix: String) -> Node:
	for c in get_tree().root.find_children("*", "", true, false):
		var s = c.get_script()
		if s != null and str(s.resource_path).ends_with(suffix):
			return c
	return null

func bag_open() -> bool:
	var n := find_by_script("EquipmentPanel.cs")
	return n != null and bool(n.get("IsOpen"))

func press(code: int) -> void:
	for down in [true, false]:
		var e := InputEventKey.new()
		e.physical_keycode = code
		e.keycode = code
		e.pressed = down
		Input.parse_input_event(e)

func finish(ok: bool, why: String) -> void:
	print("KEYTEST ", "PASS" if ok else "FAIL", " ", why)
	get_tree().quit(0 if ok else 1)

func _process(delta: float) -> void:
	t += delta
	if step == 0 and t > 25.0:   # connected, spawned, and the look picker (if any) has had its moment
		step = 1
		press(KEY_I)
	elif step == 1 and t > 27.0:
		if not bag_open():
			return finish(false, "pressing I did not open the bag")
		step = 2
		press(KEY_I)
	elif step == 2 and t > 29.0:
		finish(not bag_open(), "pressing I again closes the bag" if not bag_open() else "the bag did not close on the second I")
