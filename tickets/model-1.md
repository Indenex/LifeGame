# model-1: Model style and first upgrades
Lane: Modeling
Owner: Modeling session session_01JKX3FNjkrEkAqoTaFJiXAm (Opus)
Status: done
Branch: claude/model-1-style
PR: #172

## Goal
Every model in LifeGame is built in code today (SurfaceTool/ArrayMesh in `Avatar.cs`, `Sheep.cs`, `StructureModel*.cs`, `PotMesh.cs`, `KilnModel.cs`, `BowDrillMesh.cs`, `ItemLook.cs`, `ClaimMarkerModel.cs`, `Terrain.cs`). Done means: (1) a short art-direction note `client/assets/models/STYLE.md` (silhouette, proportions, palette, poly budget per object for 32 players on modest Linux/Windows PCs), proposed to Justin through the PM as 2 to 3 options shown as renders; (2) a pipeline decision: keep procedural meshes, or add glTF assets (say how they are made headlessly in CI-friendly tooling and committed); (3) the first visible upgrade, the player figure, then the sheep, matching the chosen style. Justin sees before/after renders.

## Progress log
- 2026-10-16: ticket written by the PM (D-073).
- 2026-10-16 17:25: started on `claude/model-1`; read Avatar.cs, Sheep.cs, Look.cs, StructureModel.cs. Render request for baseline (player, sheep, hut) sent to the Render lane.
- 2026-10-16 18:05: style prototypes pushed (65b8100): `client/scripts/models/` MeshKit (lofts, ellipsoids, leaf blades; one skinned mesh per figure), HumanRig (15-bone player skeleton, shared with Animation), HumanFigure and SheepFigure in three styles (A Carved, B Soft, C Natural), and `Showcase.tscn`, a standalone scene with no server. Local check: dotnet build green; renders checked with Godot under xvfb + lavapipe (Vulkan). Render requested 65b8100 (style options).

- 2026-10-16 18:25: rig contract with Animation (session_019roNRWQBPw4ozNLDhzseig, claude/anim-1-player-animation): their bone names and tree (Hips, Spine, Neck, Head, ShoulderL/ElbowL/HandL, ShoulderR/ElbowR/HandR, HipL/KneeL/FootL, HipR/KneeR/FootR; rest at joints, zero rotations, arms down). They proposed Node3D pivots; I proposed the same tree as Skeleton3D bones with rigid weights (1 draw call per player); Animation said yes 17:33 (option 1); sha 067ed1c sent to them for #100. HumanRig renamed to match; `PlayerModel.Build()` returns the Skeleton3D with a `ToolR` BoneAttachment3D on HandR (shaft along -Y, head toward +Z).

- 2026-10-16 18:35: PR #96 CI green on b3be674; green sha sent to the PM.
- 2026-10-16 18:40: Animation trial-merged 067ed1c with `figure = PlayerModel.Build();` in Avatar.BuildFigure: compiles, heights fine. #100 keeps capsules until Justin picks; whichever of #100 / model-1 merges second makes that one-line swap.
- 2026-10-16 19:10: D-081/D-082 from the PM: M1 order is player figure, then every item (ground, icon, held). Merged main. `ItemModels.cs`: models for 22 items (all crafting, survival and animal items plus Log), authored once in the ToolR frame, laid down for the ground and icon, plain-shaded for the hand; hooked into `ItemLook.MeshFor` (old shapes kept as `ItemLook.OldMeshFor` for fallback and before/after). Clay items keep Ceramics' ClayItemLook for now. ToolR is now a Node3D in the fist under a `HandRAttach` BoneAttachment3D. Showcase views `items` (`--old` for before) and `held`.
- 2026-10-16 19:35: clothing for Animals (D-074, worn_item slots Body 0 / Feet 1): `Garments.cs` wool tunic (200) and leather foot wraps (201), rigid-skinned on the player skeleton; `PlayerModel.Wear(figure, slot, item)` swaps them (empty body slot = leaf wrap, now its own mesh). Ground/held models for both items. `ToolL` on HandL added for Animation. Showcase view `clothes`. Crafting offers a hook for the cold hearth (FireView.cs ColdHearthView): next after renders.
- 2026-10-16 19:50: baseline renders from the Render lane at b3be674: branch claude/render-1-013qe8 commit 8f3e255, `renders/model-1-*-b3be674-*.png` (player 1-3, walk 1-2, sheep 4-5, flock 6, hut 7-8). Seen at the game camera, the player is ~60 px tall and items are specks: silhouette and size matter more than faces, which supports style A and slightly oversized raw materials. Style renders (request 3, eb8b399) are queued with the Render lane.
- 2026-10-16 20:05: Animals' wool items (animals-8, #90): drop spindle 197, wool yarn 198, wool cloth 199 modeled (by number until on main); the Showcase `items` grid shows pending ids 197-201 too. New CLAUDE.md rule: lanes never edit STATUS.md on their branches. PR #96 (made earlier at the PM's request) does; asked the PM to merge or close it.
- 2026-10-16 20:30: PM chose to close #96 (the Scribe sets my STATUS row); closed, and STATUS.md restored to main's on this branch.
- 2026-10-16 21:15: D-142 (renders never hold up merges): merged main, build green on db855b0, opened PR #172 (style A default). Avatar swap to PlayerModel.Build() happens in whichever of #172 / Animation's #100 merges second.
- 2026-10-16 21:35: hearth model on branch claude/model-1-hearth (be2d9e8, stacked on this branch): `HearthModel.Ring` (one mesh, 9 varied stones) and `HearthModel.ColdAsh`; Showcase view `hearth`. Proposed the FireView.cs hook to Crafting (default: I make it in a follow-up PR after #172).
- 2026-10-16 23:20: #172 merged by the PM (c42de30). Follow-ups continue in model-2.

## Open questions / blockers
- Sent to the PM for Justin: style A Carved (recommended) / B Soft / C Natural; pipeline: procedural C# (recommended) / Blender-made glTF.

## Style options (for Justin)
| | Look | Player tris | Sheep tris |
|---|---|---|---|
| A Carved (recommended) | flat-shaded facets, chunky 6.2-head figure, carved face planes, diamond leaves | ~1,300 | ~900 |
| B Soft | smooth and rounded, big head (5 heads), dot eyes, toy-like | ~3,000 | ~3,100 |
| C Natural | lifelike 7.3-head proportions, ears, thumbs, crimped wool, long sheep face | ~4,300 | ~3,300 |
All are one draw call per figure; 32 players at even C is ~140k tris, well inside budget, so the choice is about look.

Showcase: `godot --path client res://scripts/models/Showcase.tscn -- --view=row|people|faces|sheep|walk [--yaw=deg] [--shot=out.png]`. Headless capture here needs Vulkan (lavapipe: `apt-get install mesa-vulkan-drivers`) under `xvfb-run`; the GL path renders black.

## Next step
Done; see tickets/model-2.md. Justin's style pick (A/B/C) is still open: when it comes, set `PlayerModel.Style`.