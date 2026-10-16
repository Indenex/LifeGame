# survival-7: Running and stamina
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_progress
Branch: claude/survival-running (main after #75, plus UX's #118 branch claude/ux-ui-run-xlrh2s merged in)
PR: (none yet; open after #75 merges)

## Goal
D-088: hold the run key to move faster, spending stamina that refills whenever you aren't running. Light (D-081): running dry just drops you to the normal 4.2 m/s walk, no damage. A small ring beside the needs rings shows only while stamina isn't full. UX/UI's #118 (D-091) binds the "run" action to Shift and moves sneak to Ctrl; Animation picks the run cycle from observed speed (full run from 5.5 m/s).

## Numbers (defaults = recommended options sent to PM 2026-10-16, pending Justin)
- Run 5.6 m/s (walk 4.2, server cap World.MaxSpeed 6 unchanged). Alt: 6.0, 5.0.
- 10 s of running from full; refills in 8 s when not running; run dry and you walk until back to 20%. Alt: 20/10, 6/6.
- Indicator: stride ring beside the needs rings, fades in while not full. Alt: bar under the character.
- Running costs no food or water. Alt: hunger 1.5x while running.

## Design (technical, Survival's call)
- `shared/Stamina.cs`: constants plus `Step` and `CanRun`. Client-side only; the server keeps its 6 m/s move check, so a hacked client could run forever (accepted for now).
- `SurvivalClient.MoveSpeed(sneak, moving, walkSpeed, dt)` replaces the speed expression in `Main.Walk` (one-line hook in a shared file). Overload slows running like walking.
- Run reads the "run" action that #118 binds (Shift, rebindable). This PR gives its Controls row the "Run (hold)" Help line and requests the "run" hint once, after 8 s of steady walking.

## Progress log
- 2026-10-16: options sent to PM. Implemented defaults: shared/Stamina.cs, MoveSpeed + Running in SurvivalClient, Stride glyph and stamina ring in NeedGauge/NeedsHud, Main.Walk hook. No dotnet in this container, so not compiled locally; CI checks it once the PR opens.

- 2026-10-16: #75 merged. Merged main and #118's branch; dropped the temporary V key and the unused Running flag (Animation reads speed instead); Controls.cs run Help line and the run hint per UX/UI. Must merge with or after #118.

## Open questions / blockers
- Justin's picks on the four options above (via PM).
- If Justin picks a run speed under 5.5 m/s, tell Animation (session_019roNRWQBPw4ozNLDhzseig) to move their run band.
- Known gap: Walk() isn't called while a panel captures input, so stamina doesn't refill then. Minor.

## Next step
Wait for CI on the survival-7 PR; green: send the PM the sha (merges after #118) and a render request (stamina ring while holding Shift) to the Render lane.
