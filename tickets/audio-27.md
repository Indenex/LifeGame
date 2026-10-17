# audio-27: Digging in clay
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-27-claydig
PR: (see below)

## Goal
Digging where the ground is clay sounded the same as digging in earth. It now adds the wet suck of clay on top of the tool's own sound, and a wet slap when a spadeful is thrown.

## Progress log
- 2026-10-17: stacked on audio-26 (lamps) and audio-25 (emotes), which all add to `AudioScenes` and `AudioHooks`; retarget as each merges. Two data cues in `CueDefsClay.cs` (`DigClay`, `ClayThrow`). `ToolSounds.OnImpact` (Avatar.Impact, anim-7) plays them with the dig cue when `World.IsClay` is true at the tool point. Preview scene `clay:11` (also plays each clay cue alone for levels).
- Levels with ambience off (silence -65 dBFS): `DigClay` alone -31 beside the spade's -26; `ClayThrow` alone about -33 beside the earth throw's -33.5. Client builds, 0 errors.
- Crafting stations: nothing to add. Crafts at a table, bench, spindle or rack already sound through `SoundMix.WorkFor(processId)` (clay.* on the potter's bench, weave.* and cord.* at the table and spindle, hide.* at the rack); station use has no separate event.

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
