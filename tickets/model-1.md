# model-1: Model style and first upgrades
Lane: Modeling
Owner: Modeling session (Opus)
Status: todo
Branch: (record the harness-assigned branch here)
PR: (once opened)

## Goal
Every model in LifeGame is built in code today (SurfaceTool/ArrayMesh in `Avatar.cs`, `Sheep.cs`, `StructureModel*.cs`, `PotMesh.cs`, `KilnModel.cs`, `BowDrillMesh.cs`, `ItemLook.cs`, `ClaimMarkerModel.cs`, `Terrain.cs`). Done means: (1) a short art-direction note `client/assets/models/STYLE.md` (silhouette, proportions, palette, poly budget per object for 32 players on modest Linux/Windows PCs), proposed to Justin through the PM as 2 to 3 options shown as renders; (2) a pipeline decision: keep procedural meshes, or add glTF assets (say how they are made headlessly in CI-friendly tooling and committed); (3) the first visible upgrade, the player figure, then the sheep, matching the chosen style. Justin sees before/after renders.

## Progress log
- 2026-10-16: ticket written by the PM (D-073).

## Open questions / blockers
- (none)

## Next step
Read the mesh files listed above, render the current player, sheep and a hut through the Render lane, and draft 2 to 3 style options for Justin.
