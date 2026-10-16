# model-2: Stations, jar, cooking pot, worn clothes, new-land props
Lane: Modeling
Owner: Modeling session session_01JKX3FNjkrEkAqoTaFJiXAm (Opus)
Status: in_progress
Branch: claude/model-2 (includes claude/model-1-hearth)
PR: (once #172 merges; send green shas to the PM, D-119)

## Goal
Real models, in the model-1 style (A Carved by default) and built with `client/scripts/models/MeshKit.cs`, for: the new stations (Crafting Table, Potter's bench, distaff stand, drying rack with hides), the storage jar, the cooking pot, the tunic and foot wraps shown worn on the player figure (Garments.cs / PlayerModel.Wear, wired by Animation), and new-land props with the Environment lane. Also the cold hearth (HearthModel.cs, on claude/model-1-hearth). Each lands as a small hook in the owning lane's view file, and I tell that lane first. Justin sees renders through the Render lane, but renders never hold up merges (D-142). Never idle (D-145).

## Progress log
- 2026-10-16 21:45: started from the PM's D-145 message. Branch claude/model-2 from claude/model-1-hearth. A Sonnet survey of where each station is drawn today (crafting-stations, ux-ui-station, anim-1-stations, crafting-cook, environment-env-1/2 branches) is running.
- 2026-10-16 22:05: survey done: stations draw through `StructureModel.Pieces` (Piece lists). Crafting table is Crafting's `CraftingTableModel` on unmerged crafting-stations; Potter's bench has only `StationKind.PottersBench`, no model or structure; distaff stand and drying rack are Animals' `StationModels` on animals-wool-cloth; storage jar (`JarModel`, on main) uses Ceramics' PotMesh; no cooking pot exists anywhere; Environment env-1/2 add no new prop meshes (props are in Undergrowth.cs and Terrain.EnsureMeshes on main). Built `StationMeshes.CraftingTable()` and `StationMeshes.PottersBench()` (named to avoid Animals' `StationModels` class); Showcase view `stations`.
- 2026-10-16 22:25: `PropMeshes.cs` for Environment: Trunk (root flare, branch stubs; same base and height as Terrain's), Rock (three lumps and a moss cap), FallenLog, Stump; near-white vertex colors so Terrain's instance tint and materials apply. Showcase view `props`. Offered to Environment with Terrain.EnsureMeshes swaps. Asked Crafting what the cooking pot is.
- 2026-10-16 22:45: #172 green on c42de30 (run 38953936989), sha sent to the PM. `StationMeshes.Distaff()` and `StationMeshes.DryingRack()` (frame only; Animals' hide meshes hang at RackBarY 0.99, posts at ±1.15) added; offered to Animals (default: they swap one line each in their StructureModel switch).
- 2026-10-16 22:55: sheep in style A wired into Animals' `Sheep.cs` (BuildMeshes now takes `SheepFigure.Build(PlayerModel.Style, ...)`; the old builder kept as `Sheep.OldMesh()`). Same bones and two surfaces, so herd code is unchanged. Told Animals.
- 2026-10-16 23:05: clay items without a model are held hung from their top. Render requested 427d0cb (stations, props, hearth, sheep; plus request 3's views if not yet captured).
- 2026-10-16 23:25: #172 merged; merged main. FireView.cs hooks made (Crafting chose A): Campfire.AddRing → HearthModel.Ring, ColdHearthView ash and stubs → HearthModel.ColdAsh. `HeadAttach` (BoneAttachment3D on Head) and `PlayerModel.MouthAt` for Animation's breath puff (D-144). Potter's bench: stool moved off the +Z edge, which stays clear for Ceramics' wet pieces (ceramics-8 owns the structure; whoever merges second swaps BenchModel → StationMeshes.PottersBench). Trunk top now exactly (0, 2.8, 0) for Environment's canopies. Environment will swap Trunk/Rock and place FallenLog/Stump on env-4 after this PR is on main, and asked for birch trunk, heather, gorse, cattails, a hilltop outcrop and riverbank stones (next).

## Open questions / blockers
- Most stations are on other lanes' unmerged branches; hooks wait until those merge, so models land first as standalone meshes plus Showcase views.

## Next step
Read the station survey, then build `StationModels.cs` (Crafting Table first), with a Showcase view.
