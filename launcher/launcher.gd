extends Control
# LifeGame launcher (release-1, D-131). One button: bring the game and the world up to the
# newest green build of main, then play.
#
# Each build is a GitHub Release "build-<N>" with LifeGame-Linux.zip, LifeGame-Windows.zip,
# StdbModule.wasm and release.json (see .github/workflows/release.yml). The launcher keeps
# the game in a "LifeGame" folder next to itself and remembers which build the game and
# the world are on in LifeGame/state.json.
#
# The world is the local SpacetimeDB server on this computer. The launcher starts it if it
# is down and publishes the new module over the world with --delete-data=never, through the
# `spacetime` command and the login already on this computer. A build labelled world-wipe
# (D-130) is never published without the player saying so.
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
#   --auto               press "Update and play" at start; --no-play stops before playing,
#                        --quit exits when done (code 0 ok, 1 failed, 2 waiting on a wipe OK)
#   --screenshot=PATH    save the window to PATH once the launcher is idle, then quit

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

var state := {"client_build": 0, "server_build": 0}
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
var play_button: Button
var log_button: Button
var log_box: TextEdit
var confirm: ConfirmationDialog

func _ready() -> void:
	_parse_args()
	_build_ui()
	_load_state()
	_log("Launcher home: " + home)
	await _refresh()
	if opts.has("auto"):
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
	server_url = opts.get("server", server_url)
	db = opts.get("db", db)
	stdb_server = opts.get("spacetime-server", stdb_server)
	manage_server = not opts.has("no-server")
	home = opts.get("home", "")
	if home == "":
		if OS.has_feature("editor"):
			home = OS.get_user_data_dir()
		else:
			home = OS.get_executable_path().get_base_dir().path_join("LifeGame")
	if DirAccess.make_dir_recursive_absolute(home.path_join("downloads")) != OK:
		home = OS.get_user_data_dir().path_join("LifeGame")
		DirAccess.make_dir_recursive_absolute(home.path_join("downloads"))

func _load_state() -> void:
	var f := FileAccess.open(home.path_join("state.json"), FileAccess.READ)
	if f:
		var d = JSON.parse_string(f.get_as_text())
		if d is Dictionary:
			for k in state.keys():
				state[k] = int(d.get(k, 0))
	# A game folder without state (copied in by hand) counts as unknown, so it is replaced.
	if not FileAccess.file_exists(_game_exe()):
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
	log_button = _button("Details", false)
	log_button.pressed.connect(func(): log_box.visible = not log_box.visible)
	row.add_child(log_button)

	confirm = ConfirmationDialog.new()
	confirm.title = "This update wipes the world"
	confirm.ok_button_text = "Wipe and update"
	confirm.cancel_button_text = "Keep my world"
	confirm.dialog_autowrap = true
	confirm.min_size = Vector2i(460, 0)
	add_child(confirm)

func _small(text: String, color: Color) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", 14)
	l.add_theme_color_override("font_color", color)
	return l

func _button(text: String, primary: bool) -> Button:
	var b := Button.new()
	b.text = text
	b.custom_minimum_size = Vector2(0, 44 if primary else 36)
	b.add_theme_font_size_override("font_size", 18 if primary else 14)
	for s in ["normal", "hover", "pressed", "disabled", "focus"]:
		var st := StyleBoxFlat.new()
		st.set_corner_radius_all(8)
		st.content_margin_left = 22 if primary else 14
		st.content_margin_right = st.content_margin_left
		var base := ACCENT if primary else Color(CREAM, 0.10)
		match s:
			"hover": base = base.lightened(0.12)
			"pressed": base = base.darkened(0.15)
			"disabled": base = Color(base, 0.35)
			"focus":
				st.draw_center = false
				st.border_color = Color(CREAM, 0.5)
				st.set_border_width_all(1)
		st.bg_color = base
		b.add_theme_stylebox_override(s, st)
	b.add_theme_color_override("font_color", BG if primary else CREAM)
	b.add_theme_color_override("font_hover_color", BG if primary else CREAM)
	b.add_theme_color_override("font_disabled_color", Color(CREAM, 0.4))
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
	world_button.visible = manage_server and not latest.is_empty() and state.server_build != latest.build
	world_button.disabled = busy
	game_label.text = "Game: " + _build_name(state.client_build)
	world_label.text = ("World: " + _build_name(state.server_build)) if manage_server else "World: " + server_url

func _build_name(n: int) -> String:
	return "build %d" % n if n > 0 else "not installed" if n == 0 else "unknown"

# --- Releases ---------------------------------------------------------------------

func _refresh() -> void:
	_set_busy(true)
	_set_status("Looking for the newest build...")
	var list = await _get_json("%s/repos/%s/releases?per_page=50" % [api, repo])
	releases.clear()
	if list is Array:
		for r in list:
			var tag: String = r.get("tag_name", "")
			if r.get("draft", false) or r.get("prerelease", false) or not tag.begins_with("build-") \
					or not tag.substr(6).is_valid_int():
				continue
			var assets := {}
			for a in r.get("assets", []):
				assets[a["name"]] = a["browser_download_url"]
			var body = r.get("body", "")
			releases.append({
				"build": tag.substr(6).to_int(), "tag": tag, "title": r.get("name", tag),
				"wipe": body is String and body.contains(WIPE_MARK), "assets": assets})
		releases.sort_custom(func(a, b): return a.build > b.build)
	latest = releases[0] if not releases.is_empty() else {}
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
	confirm.dialog_text = ("Build %d starts a fresh world. Everything built, carried and stored in your current world (%s) will be gone, for everyone.\n\n" +
		"Only go ahead if the Project Manager told you this wipe is planned.") % [latest.build, from]
	var st := {"answer": false, "done": false}
	var ok := func(): st.answer = true; st.done = true
	var no := func(): st.done = true
	confirm.confirmed.connect(ok, CONNECT_ONE_SHOT)
	confirm.canceled.connect(no, CONNECT_ONE_SHOT)
	confirm.popup_centered()
	while not st.done:
		await get_tree().process_frame
	if confirm.confirmed.is_connected(ok): confirm.confirmed.disconnect(ok)
	if confirm.canceled.is_connected(no): confirm.canceled.disconnect(no)
	return st.answer

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

func _publish_world(wasm: String, wipe: bool) -> bool:
	var cli := _find_spacetime()
	if cli == "":
		_set_status("SpacetimeDB isn't installed on this computer.",
			"The world can't be updated. Install it from spacetimedb.com/install, then try again.", WARN)
		return false
	bar.value = 0.82
	if not await _ping():
		_set_status("Starting the world server...")
		var pid := OS.create_process(cli, ["start"])
		_log("Started `%s start` (pid %d)" % [cli, pid])
		var up := false
		for i in 60:
			await get_tree().create_timer(0.5).timeout
			if await _ping():
				up = true
				break
		if not up:
			_set_status("The world server didn't start.", "Run `spacetime start` in a terminal and try again.", WARN)
			return false
	bar.value = 0.86
	_set_status("Updating the world to build %d..." % latest.build, "Your world is kept." if not wipe else "Starting a fresh world.")
	var args := ["publish", db, "--server", stdb_server, "--bin-path", wasm,
		"--delete-data=" + ("always" if wipe else "never"), "--yes=migrate,break-clients,skip-login"]
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
			w.store_buffer(r.read_file(f))
			w.close()
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
	if not FileAccess.file_exists(exe):
		_set_status("The game isn't installed yet.", "", WARN)
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
