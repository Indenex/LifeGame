# audio-34: Self-registering sound schedulers and preview scenes
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-34-selfreg
PR: (see below)

## Goal
Every new Audio sound used to add a line to `AudioHooks.cs` and a scene to `AudioScenes.cs`, so each Audio PR conflicted with the next in those two files. Now a new sound or preview scene is a new file only.

## Progress log
- 2026-10-17: `AudioHooks` finds classes marked `[AudioSounds]` by reflection and calls their `Connect(DbConnection)` once and `Tick(double)` each frame (delegates cached once, in name order; a Connect that throws is logged and skipped; a Tick that throws is logged once and switched off). The ten existing schedulers (Tool, Roast, Storage, Structure, Pot, Rest, Emote, DeerGrazing, Lamp, Rabbit) are tagged. `AudioScenes` is now `partial`, one `[Scene("name")]` method per file under `tools/SoundPreview/Scenes/`, found by reflection; `Program.cs` is unchanged. All eleven existing scenes render byte-identical WAVs before and after. `docs/CONTENT.md` Sound cue row updated. Client builds, 0 errors; the reflection pattern was run in a scratch console app.
- How to add a sound now: `CueDefs<Family>.cs` for cues, `<Thing>Sounds.cs` marked `[AudioSounds]`, `Scenes/<Thing>.cs` for the preview. No shared file.

## Open questions / blockers
- The Godot client could not be run here, so the reflection in `AudioHooks` is checked by build and a scratch program, not in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
