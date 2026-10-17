# audio-8: Recorded music player (Justin brings his own music)
Lane: Audio
Owner: Audio lane (Sonnet session_01SXcxtoyeqDmypiHxkHedU6)
Status: in_review
Branch: claude/audio-8-music
PR: (see PM message)

## Goal
Justin found the synthesized music tasters "super generic" and will supply music made with Suno. So no more synthesized composition: build the player for recorded tracks. Tracks are a data registry (MusicDef: id, file, mood tags, volume trim); OGG streamed from client/assets/music/ on a Music bus (outside the synth thread); cross-fade 3 to 5 s on mood change; 75 to 200 s of silence between tracks; respects the Music on/off and slider. docs/MUSIC.md tells Justin how to add a track.

## Progress log
- 2026-10-17: Tasters (synth Score/Pieces) were rejected by Justin; removed from the branch (they stay in git history, commit "audio-8: music prototypes"). The private tasters page can be ignored.
- 2026-10-17: Built `client/scripts/atmosphere/audio/MusicDirector.cs` (pure rules: Mood day/dusk/night/rest/rain, MusicDef, MusicDefs.All, the director with 6 s mood debounce, 4 s cross-fade, 75 to 200 s gaps, first track after 15 to 45 s, no immediate repeats, fallbacks), `client/scripts/audio/MusicPlayer.cs` (Godot node: two AudioStreamPlayers on a runtime-created "Music" bus, per-track fades and trim, bus volume from the slider capped at +6 dB, missing files skipped with a log line), SoundScape adds the player and exposes the context it already receives (TimeOfDayNow, RainNow, CoverNow, FireNow). The old synthesized score is off (`SoundMix.SynthMusic` false) so the world sounds stay and the generated music is gone. SoundPreview `--music-test` checks the rules with fake tracks (empty list silent, gaps at least a minute, cross-fades 3 to 5 s, off means silent, mood rules): passes. Client builds, 0 errors. docs/MUSIC.md and client/assets/music/README.md written.
- Not run in Godot: no engine in my sandbox, so the playback node (bus creation, fades, Finished signal) is untested until a real .ogg is in the project.

## Open questions / blockers
- Waiting for Justin's music files. Until MusicDefs.All has entries, the game has no music.

## Next step
When Justin's .ogg files arrive: add them to client/assets/music/, add one MusicDef line each (docs/MUSIC.md), run the game once with each mood to check levels and the cross-fade by ear, and set trims.
