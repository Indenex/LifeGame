# crafting-34: A stopped job returns its inputs as they were
Lane: Crafting
Owner: Crafting thread
Status: in_review
Branch: crafting-refund
PR: (see below)

## Goal
crafting-31 (#614) made walking off, a panel or a change of hand end a job and give back what it took, but at the job's own output quality (close, not exact). `CraftJob` now keeps `InputQuality` (`[Default((byte)0)]`, additive), set when the job starts from the average quality of what it took; `CancelJob` returns the inputs at that quality (the vessel keeps `VesselQuality`; old rows with 0 fall back to the output quality). CraftCheck: start a cordage twist, stop it, the fiber comes back and no cordage was made.

## Progress log
- 2026-10-17: change made; builds; upgrade check and CraftCheck pass locally.

## Next step
Wait for CI, send the green sha to the PM.
