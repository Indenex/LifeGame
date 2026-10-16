# survival-7: Running and stamina
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: done
Branch: claude/survival-running (main after #75, plus UX's #118 branch claude/ux-ui-run-xlrh2s merged in)
PR: (none yet; open after #75 merges)

## Goal
D-088: hold the run key to move faster, spending stamina that refills whenever you aren't running. Light (D-081): running dry just drops you to the normal 4.2 m/s walk, no damage. A small bar under your character shows only while stamina isn't full (D-101). UX/UI's #118 (D-091) binds the "run" action to Shift and moves sneak to Ctrl; Animation picks the run cycle from observed speed (full run from 5.5 m/s).

## Numbers (defaults = recommended options sent to PM 2026-10-16, pending Justin)
- Run 5.6 m/s (Justin, D-100; walk 4.2, server cap World.MaxSpeed 6 unchanged).
- D-111 (docs/PROGRESSION.md option 1): 8 s of running from full, 12 s to refill from empty; refill waits 1 s after you stop and is faster when low (rate 1.5a at empty to 0.5a at full). Both grow linearly with rank to 16 s / 7 s at Master. A good meal (roast mutton; broth and stew once ceramics-7 lands) gives +25% run time for 10 min (Vitals.WellFedUntil, reset on death). Run dry and you walk until back to 20%.
- Indicator: small bar under the character, only while not full (Justin, D-101). client/scripts/survival/StaminaBar.cs.
- Running costs no food or water (Justin, D-101).

## Design (technical, Survival's call)
- `shared/Stamina.cs`: constants plus `Step` and `CanRun`. Client-side only; the server keeps its 6 m/s move check, so a hacked client could run forever (accepted for now).
- `SurvivalClient.MoveSpeed(sneak, moving, walkSpeed, dt)` replaces the speed expression in `Main.Walk` (one-line hook in a shared file). Overload slows running like walking.
- Run reads the "run" action that #118 binds (Shift, rebindable). This PR gives its Controls row the "Run (hold)" Help line and requests the "run" hint once, after 8 s of steady walking.

## Progress log
- 2026-10-16: options sent to PM. Implemented defaults: shared/Stamina.cs, MoveSpeed + Running in SurvivalClient, Stride glyph and stamina ring in NeedGauge/NeedsHud, Main.Walk hook. No dotnet in this container, so not compiled locally; CI checks it once the PR opens.

- 2026-10-16: #75 merged. Merged main and #118's branch; dropped the temporary V key and the unused Running flag (Animation reads speed instead); Controls.cs run Help line and the run hint per UX/UI. Must merge with or after #118.

- 2026-10-16: D-100/D-101: replaced the stamina ring with StaminaBar (46x5 px, 16 px under the feet on screen, hidden while a panel is open); NeedsHud and NeedGauge back to main.

- 2026-10-16: D-111 built: shared/Stamina.cs curves, Needs.GoodMeal, Vitals.WellFedUntil ([Default(0L)], set in Eat), client refill delay and rank hook (StaminaRank() returns 0 until the skill is picked).

- 2026-10-16: #119 green on 8a03451 (run https://github.com/Indenex/LifeGame/actions/runs/38947551748); sha sent to PM. Render requested 8a03451 (Render lane).

- 2026-10-16: #119 merged at 1be3049 (CI https://github.com/Indenex/LifeGame/actions/runs/38949413392 green).

## Open questions / blockers
- Which skill rank stamina grows with (no Survival skill exists): asked PM; options A) new basic skill Endurance trained by running [rec], B) highest basic rank, C) average of basics. Until then StaminaRank() = 0.
- ceramics-7 also edits Survival.Eat (pot comes back); whichever merges second resolves a small conflict, and Ceramics adds PotOfBroth/PotOfStew to Needs.GoodMeal.
- If Justin picks a run speed under 5.5 m/s, tell Animation (session_019roNRWQBPw4ozNLDhzseig) to move their run band.
- Known gap: Walk() isn't called while a panel captures input, so stamina doesn't refill then. Minor.

## Next step
None: merged as #119.
