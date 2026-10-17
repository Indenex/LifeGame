# animals-23: one Figure arm per model plan
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-figures-5gkntn
PR: (to open)

## Goal
D-206 housekeeping with no player-visible change: the client's `Sheep.cs` picks each model plan's joints and mesh through one `Figure(model, stag, coat)` switch (sheep, deer, hen, rabbit) instead of nested ternaries, so a new species with the shared bone layout is a Def plus one switch arm. `docs/CONTENT.md`'s Animal row now says what is data-driven today (laying, roosting, midday bedding, `DamageTaken`, `Figure`) and what remains (per-plan gait hooks, a generic voice id).

## Progress log
- 2026-10-17: built; client builds, no new warnings in Sheep.cs. Behaviour is unchanged (same joints and meshes as before).

## Open questions / blockers
- (none)

## Next step
Wait for CI, send the green sha to the PM.
