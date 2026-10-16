# survival-12: Stamina refills behind panels
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_review
Branch: claude/survival-stamina-refill (main after #185 merged)
PR: #206

## Goal
Known gap from survival-7: Main only calls Walk() (and so SurvivalClient.MoveSpeed) when no panel has the input, so the stamina bar froze while crafting or the pause menu was open. Now SurvivalClient._Process refills it on frames Walk() didn't run. Picked under D-145 while survival-10 waits on env-2.

## Progress log
- 2026-10-16: SurvivalClient tracks walkedThisFrame (Main processes before its child, so Walk runs first); Rest(dt) steps the bar with running=false otherwise. Client builds locally.

## Open questions / blockers
- (none)

## Next step
Wait for CI on #206 (opened 2026-10-16 ~19:57 UTC, head after merging main); green: send the sha to the PM (session_01CvoSQGRe3vDvDXWQu2qruU). Red: fix on claude/survival-stamina-refill.
