# ceramics-22: Pots on the ground follow a changed maker's mark
Lane: Clay and ceramics
Owner: Ceramics thread
Status: in_review
Branch: claude/clay-ceramics-22-kfr4kr
PR: (see progress log)

## Goal
When a maker adds or changes their mark, every pot they made restyles ("so older pots follow the change"). The bag and the cached meshes already did; pots lying on the ground (GroundItems batches, built once) kept the old stamp until their patch was rebuilt. They now redraw on `ClayItemLook.MarksChanged`.

## Progress log
- 2026-10-17 10:30: `GroundItems.RestyleMarked` swaps the mesh of every marked batch when marks change; client builds. Compile-checked only (no Godot here).

## Open questions / blockers
- None.

## Next step
Open the PR, send the PM the green sha.
