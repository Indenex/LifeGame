# audio-2: The new land and the seasons sound right (M1)
Lane: Audio
Owner: Audio session (session_01LeXCWTFVBQcCNVEDdhPek3, Opus)
Status: in_progress
Branch: claude/audio-2-land-seasons (2c, PR #189); claude/audio-2b-footsteps (2b, stacked); claude/audio-2a-streams (2a, stacked on 2b)
PR: (small PRs, listed below as they open)

## Goal
Three small PRs, each with a SoundPreview clip for Justin via Render and the PM:
- **2c seasons:** the ambience follows the season (Atmosphere's `Seasons`, #173): winter is windier and colder-sounding with the woods gone quiet (no birds or insects, bare branches whistle instead of leaves); autumn leaves rustle dry and crisp; spring birdsong is busier; summer days have insects buzzing and evenings have more crickets.
- **2a water:** running water along env-2's new streams (#175), with gentle lapping at the ponds as now; the sound comes from where the water is.
- **2b footsteps by surface:** grass, dirt, sand at the banks, shallow water and snow crunch (snow cover from #173).

## Progress log
- 2026-10-16 20:35: PM assigned audio-2 (M1). Branch claude/audio-2-land-seasons from main c09f9b8 + audio-1 ticket commits. Read env-2 (shared/Land.cs: RiverDistance is private) and atmosphere-16 (shared/Seasons.cs: Season, Warmth, Autumn; Atmosphere.Year/Season/SnowCover/Snowfall on the client). Footstep and water sensing live in Atmosphere.cs (Footsteps(), WaterSound()). Asked Environment for a public stream-distance call and Atmosphere to let Audio own that sensing in a partial file under atmosphere/audio/.
- 2026-10-16 21:10: 2c built: SoundMix.Year/SnowCover, SeasonSound.Of(year) (cold, summer, leaves, dry, birds, chorus, insects, frogs); winter wind slightly stronger/higher, bare-twig whistle, dry autumn leaves, snow hushes grass, NightVoices insect factor (owls kept), new DayInsects (summer drone + bee flybys), frogs by season. SoundScape.Year/SnowCover setters. SoundPreview scenes spring/summer/autumn/winter(+night). Standing levels, 40 s each, no music: spring -32.3, summer -29.9, autumn -32.5, winter -29.7 (mostly wind), nights -33 to -37. Existing scenes unchanged (storm -19.3, peak -5.2). Client + SoundPreview build. Game hook (Atmosphere sets sound.Year/SnowCover) waits for #173.
- 2026-10-16 21:30: #189 opened for 2c (head 955028d), Render requested 955028d. 2b on branch claude/audio-2b-footsteps (stacked on #189): Ground.Sand (grainy shuffle, firmer when wet) and Ground.Snow (crunch of crackles over a soft press); shallow water keeps the existing splash. SoundPreview sand/shallows/snow scenes. Loudest 50 ms of steps over their beds: grass -33.2, earth -32.5, sand -32.6, shallows -31.1, snow -30.9 dBFS. Sensing (which surface is underfoot) waits on Atmosphere's answer about moving Footsteps().
- 2026-10-16 21:50: 2a sound on branch claude/audio-2a-streams (stacked on 2b): StreamVoice (a wandering wash plus rising bubble blips spread across the stream, fuller after rain), SoundMix.Stream/StreamPan. SoundPreview stream and streamwalk scenes. Standing at the bank: stream -29.3 vs pond -32.7 dBFS rms. Sensing waits on Environment's Land.StreamDistance and Atmosphere's OK.
- 2026-10-16 21:55: Agreed. Environment: `Land.StreamDistance(int x, int z)` in #175 (meters to a stream's centre line; channel 2-3 m each side, valley 26 m; water where World.BaseHeight < World.WaterLevel; StreamDistance < ~4 is stream, other water is pond; ~10 noise lookups per call). Atmosphere: after #173 merges, move Footsteps() and WaterSound() plus stepPhase, lastPlayerPos, waterLook into atmosphere/audio/AtmosphereSound.cs (partial class Atmosphere), leaving one call each in its _Process; `sound.Foliage = wooded` and Woodedness stay Atmosphere's. Stable names: Atmosphere.Year, Season, SeasonWarmth, SnowCover, Snowfall, AirCelsius, Seasons.Autumn(year). CI run https://github.com/Indenex/LifeGame/actions/runs/38956251051 on #189 running.
- 2026-10-16 22:45: HANDOFF (D-159). State: #189 (2c seasons, branch claude/audio-2-land-seasons, head 955028d) CI green (run https://github.com/Indenex/LifeGame/actions/runs/38956251051), sent to the PM to merge. 2b (sand/snow footstep sounds) on claude/audio-2b-footsteps and 2a (StreamVoice) on claude/audio-2a-streams are pushed but not PR'd: they still need the game-side sensing, which waits for #173 and #175. Render clip for 955028d requested (Render session_013qe8zPzzBHx9rFPEYoKMr5), not yet delivered. audio-3 (cues) is stacked on 2a; see tickets/audio-3.md. Sound method and level targets: client/scripts/atmosphere/audio/AUDIO.md (on claude/audio-3-cues) and tools/SoundPreview/measure/.
- 2026-10-16 22:55: #189 (2c seasons) MERGED.

- 2026-10-16 (Sonnet takeover): #173 and #175 on main. Merged main into claude/audio-2a-streams. Moved Footsteps()/WaterSound() into client/scripts/atmosphere/audio/AtmosphereSound.cs (partial Atmosphere; Atmosphere.cs keeps terrain, Woodedness, one call each). Footsteps: Snow when SnowCover > 0.3, Sand when a cell within 3 m is below the water line. WaterSound: Land.StreamDistance < 4 is stream (sound.Stream/StreamPan), else pond. sound.Year/SnowCover set each frame. SoundScape gained Stream/StreamPan. SoundPreview.csproj excludes AtmosphereSound.cs (it uses Godot). SoundPreview builds; scenes sand/snow/stream/streamwalk render (-31.2/-30.0/-30.8 dBFS rms). The client can't be built in this sandbox (no spacetime CLI for bindings); CI will build it. One PR for 2a+2b.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the 2a+2b PR (branch claude/audio-2a-streams); send the PM the green sha and Render a clip request (`sand:12 snow:12 stream:15 streamwalk:20`); then start audio-3 (tickets/audio-3.md on claude/audio-3-cues-main).
