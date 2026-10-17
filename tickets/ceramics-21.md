# ceramics-21: Pots use the Maker column; cooking and set-down jars and troughs keep who made them (after crafting-25)
Lane: Clay and ceramics
Owner: Ceramics thread
Status: in_review
Branch: claude/clay-ceramics-20-kfr4kr
PR: (see progress log)

## Goal
Crafting's #646 added a real `Maker` column (InventoryItem, WorldItem, BasketItem, ClaimStash) that is part of a stack's identity. This ticket wires Ceramics to it and finishes the three gaps that waited on it: (1) cooking in a marked or slipped pot gives the same pot back; (2) a jar or trough set down from the build menu remembers who made it and shows the maker's stamp; (3) every place a pot changes hands carries the Maker column as well as the Wear number.

## Design
- A pot's maker and slip still travel as one number inside Ceramics' own rows (ClayPiece.Mark, KilnMarks, Lamp.Mark). On an item row the maker is ALSO in the Maker column and the Wear number keeps both, so the two readers agree and older rows (maker in Wear only) still read. Helpers `MarkFromStack(wear, maker)` and `MakerOfMark(mark)` in Ceramics.cs.
- Cooking: `CraftJob.VesselWear/VesselMaker` (appended last, with defaults) take the carried pot's identity (a peek at the stack `Take` will pick); `SimmerPot.PotWear/PotMaker` hold it; the dish item carries it, so eating gives the same pot back.
- Set-down jar/trough: new table `StructureMark(StructureId, Mark)`; `Building.Build` asks `RememberStructureMark` for the item a stage takes (3 lines). Removed with the structure. Client: `StructureMarks` registry, `StructureView` restyles the solid piece with the stamp.
- Trough items are now "marked" items (Marks.Marked), so their tooltip says who made them and the ground trough shows the stamp.

## Progress log
- 2026-10-17 10:05: server, client and ClayCheck build. Upgrade check passes (first try failed: I inserted the CraftJob columns before `Tool`; columns must be appended last).
- 2026-10-17 10:05: ClayCheck --pot checks the cooked pot comes back with its maker and slip; --kiln --jar checks the structure_mark row for the set-down jar and trough.

## Open questions / blockers
- None. Local ClayCheck runs are unreliable (the session sandbox restarts while waiting); CI is the proof.

## Next step
Open the PR, send the PM the green sha, tell UX the structure marks exist (tooltips for set-down jars).
