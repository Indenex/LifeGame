# model-36: Trading stall model
Lane: Modeling
Owner: Modeling thread
Status: blocked
Branch: claude/modeling-i8ox5v
PR: #853

## Goal
Claims' trading stall (structure kind 26, Model string "Stall") gets its own model: a small
wooden counter 1.6 m wide under a woven awning, room on the top for a few goods, in the homes'
wood and weave with two village kit crates at the seller's end. Justin sees it built in three
stages like the other structures.

## Progress log
- 2026-10-17: client/scripts/models/StallModel.cs written. `StallModel.Pieces()` returns
  StructureModel.Piece lists (stage -1 site; 0 posts and counter frame; 1 boards, top, back
  shelf; 2 rafters, woven awning with fringe as a Roof piece, kit crates with Material null).
  `StallModel.CounterTop` (0.96 m) and `StallModel.Goods` (three spots on the top) are for
  Claims to place wares. Showcase view `stall` (`--near`, `--time-of-day=0.5`) renders it.

## Open questions / blockers
- The "Stall" Model string is not in shared/Structures.cs on main yet; Claims adds the shape
  and the hook `"Stall" => StallModel.Pieces()` in StructureModel.Pieces.

## Next step
Parked: Justin picked a friends list instead of the stall. When it comes back, Claims wires `"Stall" => StallModel.Pieces()`.
wood and awning onto them.
