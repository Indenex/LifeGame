# ceramics-22: Pots on the ground follow a changed maker's mark
Lane: Clay and ceramics
Owner: Ceramics thread
Status: done
Branch: claude/clay-ceramics-22-kfr4kr
PR: #700 (merged)

## Goal
When a maker adds or changes their mark, every pot they made restyles ("so older pots follow the change"). The bag and the cached meshes already did; pots lying on the ground (GroundItems batches, built once) kept the old stamp until their patch was rebuilt. They now redraw on `ClayItemLook.MarksChanged`.

## Progress log
- 2026-10-17 10:30: `GroundItems.RestyleMarked` swaps the mesh of every marked batch when marks change; client builds. Compile-checked only (no Godot here).
- 2026-10-17 10:23: PR #700 CI green at 0c4340f (Linux and Windows clients; server jobs skipped), merged to main.

## Open questions / blockers
- None.

## Next step
Done. Ceramics lane: wait for Justin's pick (figurines A, potter's wheel B, burnish C, floor tiles D; looks drafted in tickets/ceramics-20.md), then start the first item. Later cleanup: drop ClayItemLook stand-ins for daub, grog, stone grit, clean clay, tempered clay once Modeling #744/#752 are on main.
