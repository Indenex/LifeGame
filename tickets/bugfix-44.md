# bugfix-44: "I'm stuck" command
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-44
PR: (not yet)

## Goal
Justin: "a stuck command in the menu that everyone, bots included, can use". Server reducer `unstuck` (no arguments), decided entirely on the server (D-182): finds the nearest free standing spot to the caller within 12 m (rings of 0.5 m), else a free spot near their own standing claim marker, else the world spawn area; moves them there. Free means: ground not deeper than wading, no structure footprint (grown by 0.45 m), no claim the caller has no rights in. Refused: 60 s cooldown per player ("You can shake yourself free again in N s"), while in the air ("Wait until you land"), mid-stride ("Stand still a moment first"), or when the caller already stands clear ("You don't seem to be stuck", no cooldown spent). Client: `/stuck` in chat and `Main.RequestUnstuck()` for the Esc-menu entry (UX adds the entry); shows "You shake yourself free." or the server's reason.

## Progress log
- 2026-10-17: BuildTest clay-hut passes locally with the Unstuck check (all building checks passed in 303s).
- 2026-10-17: server `Unstuck.cs`, client `movement/Main.Unstuck.cs` + `StuckCommand.cs`, BuildTest `UnstuckInHut` (bot inside the finished clay hut calls unstuck, ends outside the footprint within 12.5 m; second call inside 60 s is refused).

## Open questions / blockers
- Trunks and boulders are not checked: the server does not enforce them yet (bugfix-34). Add `Obstacles` to `StandingFree` when bugfix-34 lands.
- Standing in a finished hut counts as inside its footprint, so unstuck moves a hut dweller out. That is by design (PM spec).

## Next step
Run BuildTest clay-hut locally (log /tmp/claude-0/stk-bt.log), push, open PR, tell Integration the reducer name (`unstuck`, no arguments) and UX to add the Esc-menu entry calling `Main.RequestUnstuck()`.
