# audio-29: Sitting, lying down and getting up
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-29-lie
PR: (see below)

## Goal
Sitting down, lying down (on the ground or on a laid sleeping mat) and getting up were silent. Each now makes a soft rustle, heard by everyone within 14 m. Non-vocal.

## Progress log
- 2026-10-17: four data cues in `CueDefsRest.cs` (`RestSit`, `RestLie`, `RestLieMat` with the dry crackle of reeds, `RestUp`). `RestSounds` (client/scripts/atmosphere) watches the public `player_rest` table (Animation, anim-12; subscribed by `Subscriptions.Animation.cs`): a row inserted, or its Kind changing, is the settle (a mat when `SpotId` is set); a row deleted is the get-up. Only reducer-driven changes sound, so a player already resting when you arrive stays silent. Hooked through `AudioHooks`; preview scene `rest:15` in `AudioScenes`.
- Levels with ambience off: sit -34, lie -31 (mat -31), up -34 to -36 dBFS loudest 50 ms. Client builds, 0 errors.
- Survival's mats and beds only change `SpotId`; a bed with its own material could get its own cue by `SpotId` kind later.

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
