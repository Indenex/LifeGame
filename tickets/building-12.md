# building-12: StructureDef conversion (docs/CONTENT.md, Justin)
Lane: House building
Owner: House building thread (Sonnet)
Status: in_review
Branch: claude/house-building-8ueqz2-structdef
PR: (not yet)

## Goal
Content is data on shared bases (docs/CONTENT.md). The Structure row says: parallel `Shapes` / `StageCosts` lists that must stay in the same order, `kind` magic numbers in `StructureModel.cs`, `Structures.StageProcess`, `StepCrossesWalls`, `StructureView` and `BuildingSystem`, and one hand-written `Piece` builder per kind. Done means a new structure (or station) goes in as one def entry plus its model, with the kind number only a stable id, and the merge conflicts we just hit on three lists in a row can't happen. Behaviour is unchanged at every step.

## Plan (smallest steps first, each its own PR, no behaviour change)
1. Fold `StageCosts` into `StructureShape`: each stage becomes `{ Name, Costs }` in the shape itself. One list instead of two; delete the parallel `StageCosts` array. Server and client read the shape. (Removes the worst conflict source.)
2. Give `StructureShape` a `ModelId` (string) and look models up by id in `StructureModel.Pieces`, so the kind switch with magic numbers goes away (stations already switch on `Station`).
3. Replace the remaining kind-number checks (`StageProcess`, `StepCrossesWalls`, `StructureView.DoorFrame` `Kind == 5`) with shape flags/fields (`Room`, door frame data).
4. Update the Structure row in docs/CONTENT.md in each PR.

## Progress log
- 2026-10-16: ticket opened from Integration's message (Justin's architecture rule). Not started: building-10 (#271) and building-11 (#272) are at the merge gate for the wipe release, so the conversion waits until they land rather than rewriting shared lists under them. Plan sent to the PM as options.
- 2026-10-16: step 1 done. Each `StructureShape` now carries `Costs` next to `Stages`; `Structures.StageCosts` is derived from the shapes (`Array.ConvertAll`) and throws at start-up if a shape's stages and costs differ in length, so server and client callers are unchanged. Checked the 51 cost entries are identical to main's by script. Local: server and client build, bindings regen, upgrade-check vs origin/main passes, BuildTest (all modes) passes in 325 s. docs/CONTENT.md Structure row updated. Next kinds (Tiled house for ceramics-10, PottersBench-style stations) add one shape entry with its costs, no second list.

## Open questions / blockers
- PM: confirm order and that step 1 may start once #271/#272 are merged.

## Next step
Wait for CI on the step-1 PR, send the PM the green sha. Then step 2: a `ModelId` on StructureShape so StructureModel.Pieces looks models up by id (the kind switch with magic numbers goes away), as its own PR off main.
