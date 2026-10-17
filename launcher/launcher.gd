extends Control
# LifeGame launcher (release-1, D-131). One button: bring the game and the world up to the
# newest green build of main, then play.
#
# Each build is a GitHub Release "build-<N>-<short sha>" with LifeGame-Linux.zip, LifeGame-Windows.zip,
# StdbModule.wasm and release.json (see .github/workflows/release.yml). The launcher keeps
# the game in a "LifeGame" folder next to itself and remembers which build the game and
# the world are on in LifeGame/state.json.
#
# The world is the local SpacetimeDB server on this computer. The launcher starts it if it
# is down and publishes the new module over the world with --delete-data=never, through the
# `spacetime` command and the login already on this computer. A build labelled world-wipe
# (D-130) is never published without the player saying so.
#
# release-2: the launcher also puts a shortcut on the desktop and in the app menu on first
# run, updates itself when a release carries a different launcher (release.json "launcher"),
# starts the world server before Play, and offers SpacetimeDB's official installer when the
# `spacetime` command is missing.
#
# release-3: two launcher builds from the same release. The host launcher (above) runs the
# world server on this computer. The PLAYER launcher has no server parts at all: it updates the
# game only and connects to a server address, taken from --server, then server.cfg next to
# the launcher (or in its LifeGame folder), then the address baked in at export (server.txt).
# It is the same script; res://mode.txt says "player" in that build (or pass --player).
#
# Options, after "--" on the command line (all optional):
#   --repo=Owner/Name    where releases come from (Indenex/LifeGame)
#   --api=URL            GitHub API base (tests point it at a local fake)
#   --home=DIR           where the game and state live (LifeGame next to the launcher)
#   --spacetime=PATH     the SpacetimeDB CLI (found on PATH or in its install folder)
#   --spacetime-server=N server nickname or URL to publish to (local)
#   --server=URL         the world the game connects to (http://127.0.0.1:3000)
#   --db=NAME            database name (lifegame)
#   --no-server          only update the game; for friends joining someone else's world
#   --player             act as the player launcher (normally set by the build's mode.txt)
#   --auto               press "Update and play" at start; --no-play stops before playing,
#                        --quit exits when done (code 0 ok, 1 failed, 2 waiting on a wipe OK)
#   --press              click the main button once at start (unlike --auto, a wipe question
#                        stays open; for screenshots)
#   --screenshot=PATH    save the window to PATH once the launcher is idle, then quit
#   --no-shortcut        don't create the desktop and menu shortcuts
#   --no-self-update     never replace the launcher itself

const WIPE_MARK := "<!-- world-wipe -->"

const BG := Color(0.07, 0.08, 0.06)
const PANEL := Color(0.09, 0.10, 0.07, 0.92)
const CREAM := Color(0.97, 0.94, 0.87)
const MUTED := Color(0.97, 0.94, 0.87, 0.6)
const ACCENT := Color(0.55, 0.68, 0.38)
const WARN := Color(0.93, 0.62, 0.38)

var opts := {}
var repo := "Indenex/LifeGame"
var api := "https://api.github.com"
var home := ""
var spacetime := ""
var stdb_server := "local"
var server_url := "http://127.0.0.1:3000"
var db := "lifegame"
var manage_server := true
var player := false        # the player build: no server parts, joins a server address

var state := {"client_build": 0, "server_build": 0, "shortcut": 0}
var my_version := "dev"    # stamped by CI into res://version.txt; "dev" never self-updates
var releases: Array = []   # newest first: {build, tag, wipe, assets: {name: url}, title}
var latest := {}
var busy := false
var exit_code := 0

var status_label: Label
var detail_label: Label
var game_label: Label
var world_label: Label
var bar: ProgressBar
var main_button: Button
var world_button: Button
var refresh_button: Button
var checking := false      # a release check is running (the Refresh button says "Checking...")
var play_button: Button
var log_button: Button
var log_box: TextEdit
var wipe_overlay: Control
var wipe_text: Label
var keep_button: Button
var need_spacetime := false  # the `spacetime` command is missing; the main button installs it first

signal wipe_answered(go: bool)

func _ready() -> void:
	_parse_args()
	_build_ui()
	_load_state()
	if FileAccess.file_exists("res://version.txt"):
		my_version = FileAccess.get_file_as_string("res://version.txt").strip_edges()
	_log("Launcher %s, home: %s" % [my_version, home])
	_remove_old_launcher()
	if state.shortcut == 0 and not OS.has_feature("editor") and not opts.has("no-shortcut"):
		_make_shortcuts()
	await _refresh()
	if opts.has("press"):
		_on_main()  # like a click: a wipe question stays open, for screenshots
	if opts.has("auto"):
		if need_spacetime and manage_server and not await _install_spacetime():
			exit_code = 1
		else:
			await _update_and_play()
		if opts.has("quit"):
			get_tree().quit(exit_code)
	if opts.has("screenshot"):
		await _screenshot(opts["screenshot"])

# --- Options and state ------------------------------------------------------------

func _parse_args() -> void:
	var args := OS.get_cmdline_user_args()
	if args.is_empty():
		args = OS.get_cmdline_args()
	for a in args:
		if not a.begins_with("--"):
			continue
		var kv: PackedStringArray = a.substr(2).split("=", true, 1)
		opts[kv[0]] = kv[1] if kv.size() > 1 else ""
	repo = opts.get("repo", repo)
	api = opts.get("api", api).trim_suffix("/")
	db = opts.get("db", db)
	stdb_server = opts.get("spacetime-server", stdb_server)
	player = opts.has("player") or (FileAccess.file_exists("res://mode.txt") \
		and FileAccess.get_file_as_string("res://mode.txt").strip_edges() == "player")
	manage_server = not (opts.has("no-server") or player)
	home = opts.get("home", "")
	if home == "":
		if OS.has_feature("editor"):
			home = OS.get_user_data_dir()
		else:
			home = OS.get_executable_path().get_base_dir().path_join("LifeGame")
	if DirAccess.make_dir_recursive_absolute(home.path_join("downloads")) != OK:
		home = OS.get_user_data_dir().path_join("LifeGame")
		DirAccess.make_dir_recursive_absolute(home.path_join("downloads"))
	server_url = _server_address()

# The world the game connects to. Host: this computer (or --server). Player: --server, else the
# first line of server.cfg (next to the launcher, then in its LifeGame folder), else the address
# baked in at export; "" means none yet, and the player launcher says so instead of guessing.
func _server_address() -> String:
	if opts.has("server"):
		return str(opts["server"]).strip_edges()
	if not player:
		return server_url
	var exe_dir := OS.get_executable_path().get_base_dir()
	for path in [exe_dir.path_join("server.cfg"), home.path_join("server.cfg"), "res://server.txt"]:
		if FileAccess.file_exists(path):
			var line := FileAccess.get_file_as_string(path).strip_edges().get_slice("\n", 0).strip_edges()
			if line != "" and not line.begins_with("#"):
				return line
	return ""

func _load_state() -> void:
	var f := FileAccess.open(home.path_join("state.json"), FileAccess.READ)
	if f:
		var d = JSON.parse_string(f.get_as_text())
		if d is Dictionary:
			for k in state.keys():
				state[k] = int(d.get(k, 0))
	# A game folder without state (copied in by hand), or one with files missing (an
	# interrupted or hand-made install), counts as unknown, so it is replaced.
	if not _client_ok(home.path_join("game")):
		state["client_build"] = 0

func _save_state() -> void:
	var f := FileAccess.open(home.path_join("state.json"), FileAccess.WRITE)
	f.store_string(JSON.stringify(state, "  "))

func _is_windows() -> bool:
	return OS.get_name() == "Windows"

func _client_asset() -> String:
	return "LifeGame-Windows.zip" if _is_windows() else "LifeGame-Linux.zip"

func _game_exe() -> String:
	return home.path_join("game").path_join("LifeGame.exe" if _is_windows() else "LifeGame.x86_64")

# True when dir holds a complete game install: the executable, its LifeGame.pck and the
# data_* folder with the .NET assemblies. Godot says "Unable to find the .NET assemblies
# directory" when only the executable is there.
func _client_ok(dir: String) -> bool:
	var exe := "LifeGame.exe" if _is_windows() else "LifeGame.x86_64"
	if not FileAccess.file_exists(dir.path_join(exe)) or not FileAccess.file_exists(dir.path_join("LifeGame.pck")):
		return false
	var d := DirAccess.open(dir)
	if d == null:
		return false
	for sub in d.get_directories():
		if sub.begins_with("data_") and not DirAccess.get_files_at(dir.path_join(sub)).is_empty():
			return true
	return false

# --- UI ---------------------------------------------------------------------------

func _build_ui() -> void:
	RenderingServer.set_default_clear_color(BG)
	var bg := ColorRect.new()
	bg.color = BG
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	var panel := PanelContainer.new()
	panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	panel.offset_left = 28; panel.offset_top = 28; panel.offset_right = -28; panel.offset_bottom = -28
	var sb := StyleBoxFlat.new()
	sb.bg_color = PANEL
	sb.set_corner_radius_all(14)
	sb.set_content_margin_all(26)
	sb.border_color = Color(CREAM, 0.08)
	sb.set_border_width_all(1)
	panel.add_theme_stylebox_override("panel", sb)
	add_child(panel)

	var col := VBoxContainer.new()
	col.add_theme_constant_override("separation", 10)
	panel.add_child(col)

	var title := Label.new()
	title.text = "LifeGame"
	title.add_theme_font_size_override("font_size", 40)
	title.add_theme_color_override("font_color", CREAM)
	col.add_child(title)

	var versions := HBoxContainer.new()
	versions.add_theme_constant_override("separation", 28)
	col.add_child(versions)
	game_label = _small("", MUTED)
	world_label = _small("", MUTED)
	versions.add_child(game_label)
	versions.add_child(world_label)

	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	col.add_child(spacer)

	status_label = Label.new()
	status_label.add_theme_font_size_override("font_size", 20)
	status_label.add_theme_color_override("font_color", CREAM)
	status_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	col.add_child(status_label)
	detail_label = _small("", MUTED)
	detail_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	col.add_child(detail_label)

	bar = ProgressBar.new()
	bar.custom_minimum_size = Vector2(0, 8)
	bar.show_percentage = false
	bar.max_value = 1.0
	var track := StyleBoxFlat.new()
	track.bg_color = Color(CREAM, 0.12)
	track.set_corner_radius_all(4)
	var fill := StyleBoxFlat.new()
	fill.bg_color = ACCENT
	fill.set_corner_radius_all(4)
	bar.add_theme_stylebox_override("background", track)
	bar.add_theme_stylebox_override("fill", fill)
	col.add_child(bar)

	log_box = TextEdit.new()
	log_box.editable = false
	log_box.visible = false
	log_box.custom_minimum_size = Vector2(0, 120)
	log_box.add_theme_font_size_override("font_size", 12)
	col.add_child(log_box)

	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 10)
	col.add_child(row)
	main_button = _button("Update and play", true)
	main_button.pressed.connect(_on_main)
	row.add_child(main_button)
	play_button = _button("Play this version", false)
	play_button.pressed.connect(_play)
	row.add_child(play_button)
	world_button = _button("Update world only", false)
	world_button.pressed.connect(_on_world_only)
	row.add_child(world_button)
	var fill_space := Control.new()
	fill_space.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(fill_space)
	refresh_button = _button("Refresh", false)
	refresh_button.tooltip_text = "Check for a newer build again"
	refresh_button.pressed.connect(_on_refresh)
	row.add_child(refresh_button)
	log_button = _button("Details", false)
	log_button.pressed.connect(func(): log_box.visible = not log_box.visible)
	row.add_child(log_button)

	_build_wipe_overlay()

# The wipe question sits over the whole launcher, in its own style. "Keep my world" has the
# focus, so Enter keeps; Esc keeps too. Only a deliberate click on the other button wipes.
func _build_wipe_overlay() -> void:
	wipe_overlay = Control.new()
	wipe_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	wipe_overlay.mouse_filter = Control.MOUSE_FILTER_STOP
	wipe_overlay.visible = false
	add_child(wipe_overlay)
	var dim := ColorRect.new()
	dim.color = Color(0, 0, 0, 0.65)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	wipe_overlay.add_child(dim)
	var center := CenterContainer.new()
	center.set_anchors_preset(Control.PRESET_FULL_RECT)
	center.offset_top = 112  # below the title, so the launcher still says what it is
	center.offset_bottom = -12
	wipe_overlay.add_child(center)
	var box := PanelContainer.new()
	box.custom_minimum_size = Vector2(540, 0)
	var sb := StyleBoxFlat.new()
	sb.bg_color = Color(0.11, 0.12, 0.09)
	sb.set_corner_radius_all(14)
	sb.set_content_margin_all(26)
	sb.border_color = Color(WARN, 0.45)
	sb.set_border_width_all(1)
	box.add_theme_stylebox_override("panel", sb)
	center.add_child(box)
	var col := VBoxContainer.new()
	col.add_theme_constant_override("separation", 14)
	box.add_child(col)
	var title := Label.new()
	title.text = "This update wipes the world"
	title.add_theme_font_size_override("font_size", 22)
	title.add_theme_color_override("font_color", WARN)
	col.add_child(title)
	wipe_text = _small("", CREAM)
	wipe_text.add_theme_font_size_override("font_size", 15)
	wipe_text.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	col.add_child(wipe_text)
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 10)
	col.add_child(row)
	keep_button = _button("Keep my world", true)
	keep_button.pressed.connect(func(): wipe_answered.emit(false))
	row.add_child(keep_button)
	var gap := Control.new()
	gap.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(gap)
	var go := _button("Wipe and update", false, true)
	go.pressed.connect(func(): wipe_answered.emit(true))
	row.add_child(go)

func _unhandled_input(event: InputEvent) -> void:
	if wipe_overlay.visible and event.is_action_pressed("ui_cancel"):
		get_viewport().set_input_as_handled()
		wipe_answered.emit(false)

func _small(text: String, color: Color) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", 14)
	l.add_theme_color_override("font_color", color)
	return l

func _button(text: String, primary: bool, danger := false) -> Button:
	var b := Button.new()
	b.text = text
	b.custom_minimum_size = Vector2(0, 44 if primary else 36)
	b.add_theme_font_size_override("font_size", 18 if primary else 14)
	for s in ["normal", "hover", "pressed", "disabled", "focus"]:
		var st := StyleBoxFlat.new()
		st.set_corner_radius_all(8)
		st.content_margin_left = 22 if primary else 14
		st.content_margin_right = st.content_margin_left
		var base := ACCENT if primary else Color(WARN, 0.16) if danger else Color(CREAM, 0.10)
		match s:
			"hover": base = base.lightened(0.12) if primary else Color(base, base.a + 0.08)
			"pressed": base = base.darkened(0.15)
			"disabled": base = Color(base.darkened(0.35), base.a * 0.45)
			"focus":
				st.draw_center = false
				st.border_color = Color(CREAM, 0.5)
				st.set_border_width_all(1)
		st.bg_color = base
		b.add_theme_stylebox_override(s, st)
	var fc := BG if primary else WARN if danger else CREAM
	for k in ["font_color", "font_hover_color", "font_pressed_color", "font_focus_color"]:
		b.add_theme_color_override(k, fc)
	b.add_theme_color_override("font_disabled_color", Color(CREAM, 0.28))
	return b

func _set_status(text: String, detail := "", color := CREAM) -> void:
	status_label.text = text
	status_label.add_theme_color_override("font_color", color)
	detail_label.text = detail
	_log(text + ("  " + detail if detail != "" else ""))

func _log(line: String) -> void:
	print(line)
	if log_box:
		log_box.text += line + "\n"
		log_box.scroll_vertical = log_box.get_line_count()

func _set_busy(on: bool) -> void:
	busy = on
	_update_buttons()

func _update_buttons() -> void:
	var have_game := FileAccess.file_exists(_game_exe())
	var up_to_date: bool = not latest.is_empty() and state.client_build == latest.build \
		and (not manage_server or state.server_build == latest.build)
	main_button.text = "Play" if up_to_date else "Update and play"
	main_button.disabled = busy or (latest.is_empty() and not have_game)
	play_button.visible = have_game and not up_to_date
	play_button.disabled = busy
	world_button.visible = manage_server and not need_spacetime and not latest.is_empty() and state.server_build != latest.build
	if need_spacetime and manage_server:
		main_button.text = "Install SpacetimeDB and play"
		main_button.disabled = busy
		play_button.visible = false
	world_button.disabled = busy
	refresh_button.disabled = busy
	refresh_button.text = "Checking..." if checking else "Refresh"
	game_label.text = "Game: " + _build_name(state.client_build)
	if manage_server:
		world_label.text = "World: " + _build_name(state.server_build)
	else:
		world_label.text = "Server: " + (server_url if server_url != "" else "no address yet")

func _build_name(n: int) -> String:
	return "build %d" % n if n > 0 else "not installed" if n == 0 else "unknown"

# --- Releases ---------------------------------------------------------------------

# The Refresh button: look at the releases again without restarting (new builds appear while
# the launcher is open). Ignored while anything else is running.
func _on_refresh() -> void:
	if busy:
		return
	await _refresh()

func _refresh() -> void:
	checking = true
	_set_busy(true)
	_set_status("Looking for the newest build...")
	var list = await _get_json("%s/repos/%s/releases?per_page=50" % [api, repo])
	releases.clear()
	if list is Array:
		for r in list:
			# Tags are build-<number>-<short sha>; the number orders them.
			var tag: String = r.get("tag_name", "")
			var num := tag.substr(6).get_slice("-", 0)
			if r.get("draft", false) or r.get("prerelease", false) or not tag.begins_with("build-") \
					or not num.is_valid_int():
				continue
			var assets := {}
			for a in r.get("assets", []):
				assets[a["name"]] = a["browser_download_url"]
			var body = r.get("body", "")
			releases.append({
				"build": num.to_int(), "tag": tag, "title": r.get("name", tag),
				"wipe": body is String and body.contains(WIPE_MARK), "assets": assets})
		releases.sort_custom(func(a, b): return a.build > b.build)
	latest = releases[0] if not releases.is_empty() else {}
	if not latest.is_empty() and await _self_update():
		return  # a new launcher is starting in our place
	checking = false
	_set_busy(false)
	if latest.is_empty():
		if list == null:
			_set_status("Can't reach GitHub right now.", "You can still play the version you have.", WARN)
		else:
			_set_status("No builds have been released yet.", "", WARN)
	elif state.client_build == latest.build and (not manage_server or state.server_build == latest.build):
		_set_status("Everything is up to date.", "Build %d" % latest.build)
	else:
		var d := "Build %d is ready." % latest.build
		if _wipe_ahead():
			d += "  It wipes the world; you'll be asked first."
		_set_status("An update is ready.", d)
	if manage_server and _find_spacetime() == "":
		_needs_spacetime()

func _wipe_ahead() -> bool:
	if not manage_server or latest.is_empty():
		return false
	for r in releases:
		if r.wipe and r.build > state.server_build and r.build <= latest.build:
			return true
	return false

func _get_json(url: String):
	var req := HTTPRequest.new()
	req.timeout = 30
	add_child(req)
	var err := req.request(url, ["User-Agent: LifeGame-Launcher", "Accept: application/vnd.github+json"])
	if err != OK:
		req.queue_free()
		return null
	var res: Array = await req.request_completed
	req.queue_free()
	if res[0] != HTTPRequest.RESULT_SUCCESS or res[1] != 200:
		_log("GET %s failed (result %d, HTTP %d)" % [url, res[0], res[1]])
		return null
	return JSON.parse_string(res[3].get_string_from_utf8())

# Downloads url to path with the progress bar between from and to. Returns true on success.
func _download(url: String, path: String, what: String, from: float, to: float) -> bool:
	var req := HTTPRequest.new()
	req.download_file = path + ".part"
	req.download_chunk_size = 1 << 18
	req.max_redirects = 8
	add_child(req)
	var st := {"done": false, "result": [] as Array}
	req.request_completed.connect(func(r, c, _h, _b): st.result = [r, c]; st.done = true)
	if req.request(url, ["User-Agent: LifeGame-Launcher"]) != OK:
		req.queue_free()
		return false
	while not st.done:
		var total := req.get_body_size()
		var got := req.get_downloaded_bytes()
		if total > 0:
			bar.value = lerpf(from, to, float(got) / total)
			detail_label.text = "%s  %.0f of %.0f MB" % [what, got / 1048576.0, total / 1048576.0]
		else:
			detail_label.text = "%s  %.0f MB" % [what, got / 1048576.0]
		await get_tree().process_frame
	req.queue_free()
	if st.result[0] != HTTPRequest.RESULT_SUCCESS or st.result[1] != 200:
		_log("Download of %s failed (result %d, HTTP %d)" % [url, st.result[0], st.result[1]])
		DirAccess.remove_absolute(path + ".part")
		return false
	DirAccess.remove_absolute(path)
	DirAccess.rename_absolute(path + ".part", path)
	bar.value = to
	return true

# Fetches the release's files into downloads/, checked against release.json.
func _fetch(rel: Dictionary, names: Array, from: float, to: float) -> Dictionary:
	var dir := home.path_join("downloads").path_join(rel.tag)
	DirAccess.make_dir_recursive_absolute(dir)
	var manifest_path := dir.path_join("release.json")
	if not rel.assets.has("release.json") or not await _download(rel.assets["release.json"], manifest_path, "release.json", from, from):
		return {}
	var manifest = JSON.parse_string(FileAccess.get_file_as_string(manifest_path))
	if not manifest is Dictionary:
		return {}
	var sums := {manifest.server.file: manifest.server.sha256,
		manifest.client.linux.file: manifest.client.linux.sha256,
		manifest.client.windows.file: manifest.client.windows.sha256}
	var out := {}
	var step: float = (to - from) / max(names.size(), 1)
	for i in names.size():
		var n: String = names[i]
		var p := dir.path_join(n)
		var a: float = from + step * i
		if not (FileAccess.file_exists(p) and FileAccess.get_sha256(p) == sums.get(n, "")):
			if not rel.assets.has(n) or not await _download(rel.assets[n], p, "Downloading " + n, a, a + step):
				_set_status("Download failed.", "Check the internet connection and try again.", WARN)
				return {}
			if FileAccess.get_sha256(p) != sums.get(n, ""):
				DirAccess.remove_absolute(p)
				_set_status("A download came out damaged.", "Try again; it will download fresh.", WARN)
				return {}
		out[n] = p
	return out

# --- Actions ----------------------------------------------------------------------

func _on_main() -> void:
	if need_spacetime and manage_server:
		if not await _install_spacetime():
			return
	if main_button.text == "Play":
		_play()
	else:
		await _update_and_play()

func _on_world_only() -> void:
	await _update(false, true)

func _update_and_play() -> void:
	if latest.is_empty():
		if FileAccess.file_exists(_game_exe()) and not opts.has("no-play"):
			_play()
		return
	if await _update(true, manage_server) and not opts.has("no-play"):
		_play()

# Brings the game and/or the world to the newest build. Downloads everything first, then
# publishes the world, and only then swaps the game, so the two never end up mismatched.
func _update(client: bool, world: bool) -> bool:
	if busy:
		return false
	var need_client: bool = client and state.client_build != latest.build
	var need_world: bool = world and state.server_build != latest.build
	if not need_client and not need_world:
		return true
	_set_busy(true)
	exit_code = 0
	var wipe := false
	if need_world and _wipe_ahead():
		if opts.has("auto"):
			_set_status("This update wipes the world.", "Open the launcher and choose.", WARN)
			exit_code = 2
			_set_busy(false)
			return false
		wipe = await _ask_wipe()
		if not wipe:
			_set_status("Nothing changed.", "Your world and game stay on build %d." % state.server_build)
			_set_busy(false)
			return false

	var names := []
	if need_world: names.append("StdbModule.wasm")
	if need_client: names.append(_client_asset())
	_set_status("Downloading build %d..." % latest.build)
	var files := await _fetch(latest, names, 0.0, 0.8)
	if files.is_empty():
		return _fail()

	if need_world:
		if not await _publish_world(files["StdbModule.wasm"], wipe):
			return _fail()
		state.server_build = latest.build
		_save_state()
	if need_client:
		_set_status("Installing the game...")
		if not await _install_client(files[_client_asset()]):
			_set_status("Couldn't install the game.", "Is the game still open? Close it and try again.", WARN)
			return _fail()
		state.client_build = latest.build
		_save_state()
	bar.value = 1.0
	_set_busy(false)
	_set_status("Everything is up to date.", "Build %d" % latest.build)
	return true

func _fail() -> bool:
	exit_code = 1
	_set_busy(false)
	return false

func _ask_wipe() -> bool:
	var from := ("build %d" % state.server_build) if state.server_build > 0 else "the build it's on"
	wipe_text.text = ("Build %d starts a fresh world. Everything built, carried and stored in your current world (%s) will be gone, for everyone.\n\n" +
		"Only go ahead if the Project Manager told you this wipe is planned.") % [latest.build, from]
	wipe_overlay.visible = true
	keep_button.grab_focus()
	var go: bool = await wipe_answered
	wipe_overlay.visible = false
	return go

# --- World (SpacetimeDB) ----------------------------------------------------------

func _find_spacetime() -> String:
	if spacetime != "":
		return spacetime
	var cands: Array = []
	if opts.has("spacetime"): cands.append(opts["spacetime"])
	cands.append_array(["spacetime", "spacetimedb-cli"])
	if _is_windows():
		cands.append(OS.get_environment("LOCALAPPDATA").path_join("SpacetimeDB").path_join("spacetime.exe"))
	else:
		cands.append(OS.get_environment("HOME").path_join(".local/bin/spacetime"))
	for c in cands:
		var out := []
		if OS.execute(c, ["--version"], out, true) == 0:
			spacetime = c
			_log("SpacetimeDB CLI: %s (%s)" % [c, "".join(out).strip_edges().split("\n")[0]])
			return c
	return ""

func _ping() -> bool:
	var req := HTTPRequest.new()
	req.timeout = 3
	add_child(req)
	if req.request(server_url.trim_suffix("/") + "/v1/ping") != OK:
		req.queue_free()
		return false
	var res: Array = await req.request_completed
	req.queue_free()
	return res[0] == HTTPRequest.RESULT_SUCCESS and res[1] < 500

# Starts the local world server when it isn't answering. True once it answers.
func _ensure_server() -> bool:
	if await _ping():
		return true
	var cli := _find_spacetime()
	if cli == "":
		_needs_spacetime()
		return false
	_set_status("Starting the world server...")
	var pid := OS.create_process(cli, ["start"])
	_log("Started `%s start` (pid %d)" % [cli, pid])
	for i in 60:
		await get_tree().create_timer(0.5).timeout
		if await _ping():
			return true
	_set_status("The world server didn't start.", "Details has what the launcher tried.", WARN)
	return false

func _needs_spacetime() -> void:
	_set_status("SpacetimeDB isn't installed on this computer.",
		"The world runs on it. The button below installs it (about a minute), then carries on.", WARN)
	need_spacetime = true
	_update_buttons()

# Runs SpacetimeDB's official installer (https://spacetimedb.com/install). On Windows its own
# installer window opens and asks once; on Linux it installs to ~/.local/bin without asking.
func _install_spacetime() -> bool:
	if busy:
		return false
	_set_busy(true)
	_set_status("Installing SpacetimeDB...", "Downloading the official installer.")
	var res: Dictionary
	if _is_windows():
		res = await _run("powershell", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command",
			"irm https://windows.spacetimedb.com | iex"])
	else:
		res = await _run("sh", ["-c", "curl -sSf https://install.spacetimedb.com | sh -s -- --yes"])
	_log(res.output)
	spacetime = ""
	_set_busy(false)
	if _find_spacetime() == "":
		_set_status("SpacetimeDB didn't install.", "Details has the installer's message.", WARN)
		log_box.visible = true
		return false
	need_spacetime = false
	_update_buttons()
	_set_status("SpacetimeDB is installed.")
	return true

func _publish_world(wasm: String, wipe: bool) -> bool:
	var cli := _find_spacetime()
	if cli == "":
		_needs_spacetime()
		return false
	bar.value = 0.82
	if not await _ensure_server():
		return false
	bar.value = 0.86
	_set_status("Updating the world to build %d..." % latest.build, "Your world is kept." if not wipe else "Starting a fresh world.")
	# SpacetimeDB 2.12 asks "This will DESTROY ... ALL corresponding data" before a wipe and
	# aborts without a terminal unless --yes names delete-data. Only the wipe path (which the
	# player confirmed in the dialog) passes it; a normal update never skips that prompt.
	var yes := "migrate,break-clients,skip-login" + (",delete-data" if wipe else "")
	var args := ["publish", db, "--server", stdb_server, "--bin-path", wasm,
		"--delete-data=" + ("always" if wipe else "never"), "--yes=" + yes]
	_log("> %s %s" % [cli, " ".join(args)])
	var res := await _run(cli, args)
	_log(res.output)
	if res.code != 0:
		if not wipe and (res.output.to_lower().contains("delete-data") or res.output.to_lower().contains("migrat")):
			_set_status("This build can't update your world without wiping it.",
				"Nothing changed; your world is safe. Tell the Project Manager thread.", WARN)
		else:
			_set_status("The world update failed. Nothing changed.", "Details has the server's message.", WARN)
			log_box.visible = true
		return false
	bar.value = 0.9
	return true

# Runs a command on a thread so the window keeps drawing. Returns {code, output}.
func _run(cmd: String, args: Array) -> Dictionary:
	var t := Thread.new()
	t.start(func():
		var out := []
		var code := OS.execute(cmd, args, out, true)
		return {"code": code, "output": "".join(out)})
	while t.is_alive():
		await get_tree().process_frame
	return t.wait_to_finish()

# --- Game -------------------------------------------------------------------------

# Unpacks into game.new, then swaps it in for game; the old game stays until the swap works.
func _install_client(zip: String) -> bool:
	var game := home.path_join("game")
	var fresh := home.path_join("game.new")
	var old := home.path_join("game.old")
	_remove_tree(fresh)
	_remove_tree(old)
	var t := Thread.new()
	var progress := {"done": 0, "total": 1}
	t.start(_unzip.bind(zip, fresh, progress))
	while t.is_alive():
		bar.value = lerpf(0.9, 0.99, float(progress.done) / max(progress.total, 1))
		detail_label.text = "Unpacking %d of %d files" % [progress.done, progress.total]
		await get_tree().process_frame
	if not t.wait_to_finish():
		_log("The game zip didn't unpack completely (%d of %d files)." % [progress.done, progress.total])
		return false
	# Never swap in an install that is missing pieces; the old game stays until this is whole.
	if not _client_ok(fresh):
		_log("The unpacked game is missing LifeGame.pck, the executable or the data_* folder.")
		_remove_tree(fresh)
		return false
	if not _is_windows():
		OS.execute("chmod", ["+x", fresh.path_join("LifeGame.x86_64")])
	if DirAccess.dir_exists_absolute(game) and DirAccess.rename_absolute(game, old) != OK:
		return false
	if DirAccess.rename_absolute(fresh, game) != OK:
		DirAccess.rename_absolute(old, game)
		return false
	_remove_tree(old)
	return true

func _unzip(zip: String, dest: String, progress: Dictionary) -> bool:
	var r := ZIPReader.new()
	if r.open(zip) != OK:
		return false
	var files := r.get_files()
	progress.total = files.size()
	for f in files:
		var target := dest.path_join(f)
		if not target.simplify_path().begins_with(dest.simplify_path()):
			continue  # never write outside the game folder
		if f.ends_with("/"):
			DirAccess.make_dir_recursive_absolute(target)
		else:
			DirAccess.make_dir_recursive_absolute(target.get_base_dir())
			var w := FileAccess.open(target, FileAccess.WRITE)
			if w == null:
				return false
			var data := r.read_file(f)
			w.store_buffer(data)
			var wrote := w.get_length()
			w.close()
			if wrote != data.size():
				return false  # a short write (disk full?) must not count as installed
		progress.done += 1
	r.close()
	return true

func _remove_tree(path: String) -> void:
	if not DirAccess.dir_exists_absolute(path):
		return
	var d := DirAccess.open(path)
	d.include_hidden = true
	for f in d.get_files():
		DirAccess.remove_absolute(path.path_join(f))
	for sub in d.get_directories():
		_remove_tree(path.path_join(sub))
	DirAccess.remove_absolute(path)

func _play() -> void:
	var exe := _game_exe()
	if not _client_ok(home.path_join("game")):
		# Files are missing (or the game was never installed): fetch it again before playing.
		state.client_build = 0
		_save_state()
		if latest.is_empty() or not await _update(true, false) or not _client_ok(home.path_join("game")):
			_set_status("The game isn't installed yet.", "Press Update and play when you're online.", WARN)
			return
	if player and server_url == "":
		_set_status("No server address yet.", "Ask the host for it, then put it in server.cfg next to the launcher.", WARN)
		return
	if manage_server:
		_set_busy(true)
		var up := await _ensure_server()
		_set_busy(false)
		if not up:
			return
	var args := ["--", "--server=" + server_url, "--db=" + db]
	var pid := OS.create_process(exe, args)
	if pid <= 0:
		_set_status("The game didn't start.", exe, WARN)
		return
	_log("Started the game (pid %d)" % pid)
	_set_status("Have a good evening in the woods.")
	await get_tree().create_timer(1.5).timeout
	get_tree().quit()

func _screenshot(path: String) -> void:
	while busy:
		await get_tree().process_frame
	await RenderingServer.frame_post_draw
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(path)
	get_tree().quit()

# --- The launcher itself ----------------------------------------------------------

# The shortcut's name: the two builds can sit side by side.
func _app_name() -> String:
	return "LifeGame (player)" if player else "LifeGame"

func _launcher_exe_name() -> String:
	return "LifeGameLauncher.exe" if _is_windows() else "LifeGameLauncher.x86_64"

# Windows can rename a running exe but not delete it, so the swap leaves the old one behind.
func _remove_old_launcher() -> void:
	if OS.has_feature("editor"):
		return
	var old := OS.get_executable_path().get_basename() + ".old" + ("." + OS.get_executable_path().get_extension() if _is_windows() else "")
	if FileAccess.file_exists(old):
		DirAccess.remove_absolute(old)

# Replaces this launcher with the release's when their versions differ, then starts the new
# one with the same options. Returns true when the new launcher has been started.
func _self_update() -> bool:
	if my_version == "dev" or opts.has("no-self-update") or opts.has("updated-from") or OS.has_feature("editor"):
		return false
	var dir := home.path_join("downloads").path_join(latest.tag)
	DirAccess.make_dir_recursive_absolute(dir)
	var manifest_path := dir.path_join("release.json")
	if not latest.assets.has("release.json") or not await _download(latest.assets["release.json"], manifest_path, "release.json", 0, 0):
		return false
	var manifest = JSON.parse_string(FileAccess.get_file_as_string(manifest_path))
	if not (manifest is Dictionary and manifest.has("launcher")):
		return false
	var info: Dictionary = manifest.launcher
	if str(info.get("version", "")) in ["", "unknown", my_version]:
		return false
	# The player launcher updates to the player build, the host launcher to the host build.
	var section: Dictionary = info.get("player", {}) if player else info
	if not (section.has("windows") and section.has("linux")):
		_log("Launcher update skipped: this release has no %s launcher" % ("player" if player else "host"))
		return false
	var entry: Dictionary = section.windows if _is_windows() else section.linux
	var zip := dir.path_join(entry.file)
	_set_status("Updating the launcher...", "It restarts by itself.")
	if not (FileAccess.file_exists(zip) and FileAccess.get_sha256(zip) == entry.sha256):
		if not latest.assets.has(entry.file) or not await _download(latest.assets[entry.file], zip, "Downloading the new launcher", 0, 1):
			_log("Launcher update skipped: download failed")
			return false
		if FileAccess.get_sha256(zip) != entry.sha256:
			_log("Launcher update skipped: download damaged")
			return false
	var fresh := home.path_join("launcher.new")
	_remove_tree(fresh)
	if not _unzip(zip, fresh, {"done": 0, "total": 1}):
		return false
	var new_exe := fresh.path_join(entry.exe)
	var exe := OS.get_executable_path()
	var old := exe.get_basename() + ".old" + ("." + exe.get_extension() if _is_windows() else "")
	DirAccess.remove_absolute(old)
	if DirAccess.rename_absolute(exe, old) != OK:
		_log("Launcher update skipped: can't move " + exe)
		return false
	if DirAccess.rename_absolute(new_exe, exe) != OK and DirAccess.copy_absolute(new_exe, exe) != OK:
		DirAccess.rename_absolute(old, exe)
		_log("Launcher update skipped: can't place the new launcher")
		return false
	_remove_tree(fresh)
	if not _is_windows():
		OS.execute("chmod", ["+x", exe])
		DirAccess.remove_absolute(old)  # Linux keeps the running file alive without its name
	# Engine options such as --headless aren't in get_cmdline_args, so carry that one over.
	var args: Array = ["--headless"] if DisplayServer.get_name() == "headless" else []
	args.append_array(Array(OS.get_cmdline_args()).filter(func(x): return x != "--headless"))
	args.append_array(["--"] + Array(OS.get_cmdline_user_args()) + ["--updated-from=" + my_version])
	_log("Restarting with " + " ".join(args))
	var pid := OS.create_process(exe, args)
	if pid <= 0:
		_log("The new launcher didn't start")
		return false
	_log("Launcher %s -> %s (pid %d)" % [my_version, info.version, pid])
	get_tree().quit()
	return true

# First run: a LifeGame shortcut on the desktop and in the app menu, pointing at this launcher.
func _make_shortcuts() -> void:
	var exe := OS.get_executable_path()
	var icon := home.path_join("icon.png")
	var tex := load("res://icon.png") as Texture2D
	if tex:
		tex.get_image().save_png(icon)
	var ok := false
	if _is_windows():
		var ico := exe  # the exe carries the icon
		var ps := ("$w = New-Object -ComObject WScript.Shell; " +
			"foreach ($d in @([Environment]::GetFolderPath('Desktop'), (Join-Path ([Environment]::GetFolderPath('Programs')) ''))) { " +
			"$s = $w.CreateShortcut((Join-Path $d '%s.lnk')); $s.TargetPath = '%s'; " +
			"$s.WorkingDirectory = '%s'; $s.IconLocation = '%s,0'; $s.Description = 'LifeGame'; $s.Save() }") % [
			_app_name(), exe.replace("/", "\\").replace("'", "''"), exe.get_base_dir().replace("/", "\\").replace("'", "''"),
			ico.replace("/", "\\").replace("'", "''")]
		ok = OS.execute("powershell", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", ps]) == 0
	else:
		var entry := "[Desktop Entry]\nType=Application\nName=%s\nComment=Update and play LifeGame\n" % _app_name() + \
			"Exec=\"%s\"\nPath=%s\nIcon=%s\nTerminal=false\nCategories=Game;\n" % [exe, exe.get_base_dir(), icon]
		var home_dir := OS.get_environment("HOME")
		var apps := OS.get_environment("XDG_DATA_HOME")
		apps = (apps if apps != "" else home_dir.path_join(".local/share")).path_join("applications")
		var out := []
		OS.execute("xdg-user-dir", ["DESKTOP"], out)
		var desktop: String = "".join(out).strip_edges()
		if desktop == "" or desktop == home_dir:
			desktop = home_dir.path_join("Desktop")
		for d in [apps, desktop]:
			if not DirAccess.dir_exists_absolute(d):
				if d == desktop:
					continue
				DirAccess.make_dir_recursive_absolute(d)
			var path: String = d.path_join("lifegame-player.desktop" if player else "lifegame.desktop")
			var f := FileAccess.open(path, FileAccess.WRITE)
			if f == null:
				continue
			f.store_string(entry)
			f.close()
			OS.execute("chmod", ["+x", path])
			OS.execute("gio", ["set", path, "metadata::trusted", "true"])  # GNOME: allow launching
			ok = true
	_log("Shortcuts: " + ("made" if ok else "couldn't be made"))
	if ok:
		state.shortcut = 1
		_save_state()
