# bugfix-52: Suspicion log
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-52
PR: (see below)

## Goal
Audit item 14 of bugfix-24. Where the server already accepts a call and clamps what is wrong with it, it also counts it in a private `Suspicion` table (`Suspicion.cs`), one row per player and kind with a count and first/last time. A refused call throws and rolls everything back, so refusals are not logged; only tolerated ones. Kinds: MoveOverBudget (asked for more than 0.75 m beyond the walking budget), MoveBlocked (step into a wall, trunk and the like, sent back), QuickScore (minigame score above the quick cap with no real play time), PerfectStreak (perfect score pulled down by the streak cap). The host reads the table with `spacetime sql`; admins read it through the `suspicion_report` view; other players see nothing. Nothing about play changes.

Audit item 5 (crafting score) is covered by Crafting's #780 (BeginMinigame, MinigameStart, CraftScore.cs: minimum play time and perfect-streak soft cap).

## Progress log
- 2026-10-17: Suspicion.cs, hooks in Move and CraftScore; server builds; ctx.Sender check passes.
- Local: SpawnCheck passes; `spacetime sql` shows MoveBlocked rows from its deliberate stone probe, no MoveOverBudget for honest walking.

## Open questions / blockers
- Rows only count; there is no pruning or in-game admin panel yet (a table of at most players x 4 rows, so small).
- No CI test reads the table (clients cannot read it); checked by hand with spacetime sql.

## Next step
Wait for PR CI, then send the PM the green sha.
