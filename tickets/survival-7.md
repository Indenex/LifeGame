# survival-7: Running and stamina
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_progress
Branch: claude/survival-running (stacked on survival-5 / #75 via claude/survival-numbers)
PR: (none yet; open after #75 merges)

## Goal
D-088: hold the run key to move faster, spending stamina that refills whenever you aren't running. Light (D-081): running dry just drops you to the normal 4.2 m/s walk, no damage. A small ring beside the needs rings shows only while stamina isn't full. Sneak moves off Shift (UX/UI picks its key) and run takes Shift; Animation adds the run cycle (reads `SurvivalClient.Running`).

## Numbers (defaults = recommended options sent to PM 2026-10-16, pending Justin)
- Run 5.6 m/s (walk 4.2, server cap World.MaxSpeed 6 unchanged). Alt: 6.0, 5.0.
- 10 s of running from full; refills in 8 s when not running; run dry and you walk until back to 20%. Alt: 20/10, 6/6.
- Indicator: stride ring beside the needs rings, fades in while not full. Alt: bar under the character.
- Running costs no food or water. Alt: hunger 1.5x while running.

## Design (technical, Survival's call)
- `shared/Stamina.cs`: constants plus `Step` and `CanRun`. Client-side only; the server keeps its 6 m/s move check, so a hacked client could run forever (accepted for now).
- `SurvivalClient.MoveSpeed(sneak, moving, walkSpeed, dt)` replaces the speed expression in `Main.Walk` (one-line hook in a shared file). Overload slows running like walking.
- Run is on a temporary V key ("run" action) until UX/UI moves sneak off Shift; then they rebind run to Shift and update KEYMAP.md / Controls.cs.

## Progress log
- 2026-10-16: options sent to PM. Implemented defaults: shared/Stamina.cs, MoveSpeed + Running in SurvivalClient, Stride glyph and stamina ring in NeedGauge/NeedsHud, Main.Walk hook. No dotnet in this container, so not compiled locally; CI checks it once the PR opens.

## Open questions / blockers
- Justin's picks on the four options above (via PM).
- UX/UI: sneak's new key, then bind run to Shift (via PM).
- Known gap: Walk() isn't called while a panel captures input, so stamina doesn't refill then. Minor.

## Next step
After #75 merges: merge main into claude/survival-running, open the survival-7 PR, wait for CI, then send a render request (ring while running, V held) to the Render lane.
