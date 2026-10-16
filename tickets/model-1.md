# model-1: Model style and first upgrades
Lane: Modeling
Owner: Modeling session session_01JKX3FNjkrEkAqoTaFJiXAm (Opus)
Status: in_progress
Branch: claude/model-1
PR: (once opened)

## Goal
Every model in LifeGame is built in code today (SurfaceTool/ArrayMesh in `Avatar.cs`, `Sheep.cs`, `StructureModel*.cs`, `PotMesh.cs`, `KilnModel.cs`, `BowDrillMesh.cs`, `ItemLook.cs`, `ClaimMarkerModel.cs`, `Terrain.cs`). Done means: (1) a short art-direction note `client/assets/models/STYLE.md` (silhouette, proportions, palette, poly budget per object for 32 players on modest Linux/Windows PCs), proposed to Justin through the PM as 2 to 3 options shown as renders; (2) a pipeline decision: keep procedural meshes, or add glTF assets (say how they are made headlessly in CI-friendly tooling and committed); (3) the first visible upgrade, the player figure, then the sheep, matching the chosen style. Justin sees before/after renders.

## Progress log
- 2026-10-16: ticket written by the PM (D-073).
- 2026-10-16 17:25: started on `claude/model-1`; read Avatar.cs, Sheep.cs, Look.cs, StructureModel.cs. No Godot or dotnet in this container, so the Render lane builds and captures.

## Open questions / blockers
- (none)

## Next step
Wait for the Render lane's baseline renders (player, sheep, hut); meanwhile build the style-option prototypes in `client/scripts/models/`.
