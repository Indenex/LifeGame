# model-1: Model style and first upgrades
Lane: Modeling
Owner: Modeling session session_01JKX3FNjkrEkAqoTaFJiXAm (Opus)
Status: in_progress
Branch: claude/model-1 (bookkeeping, PR #96); style work on claude/model-1-style (stacked)
PR: #96 (bookkeeping)

## Goal
Every model in LifeGame is built in code today (SurfaceTool/ArrayMesh in `Avatar.cs`, `Sheep.cs`, `StructureModel*.cs`, `PotMesh.cs`, `KilnModel.cs`, `BowDrillMesh.cs`, `ItemLook.cs`, `ClaimMarkerModel.cs`, `Terrain.cs`). Done means: (1) a short art-direction note `client/assets/models/STYLE.md` (silhouette, proportions, palette, poly budget per object for 32 players on modest Linux/Windows PCs), proposed to Justin through the PM as 2 to 3 options shown as renders; (2) a pipeline decision: keep procedural meshes, or add glTF assets (say how they are made headlessly in CI-friendly tooling and committed); (3) the first visible upgrade, the player figure, then the sheep, matching the chosen style. Justin sees before/after renders.

## Progress log
- 2026-10-16: ticket written by the PM (D-073).
- 2026-10-16 17:25: started on `claude/model-1`; read Avatar.cs, Sheep.cs, Look.cs, StructureModel.cs. Render request for baseline (player, sheep, hut) sent to the Render lane.
- 2026-10-16 18:05: style prototypes pushed (65b8100): `client/scripts/models/` MeshKit (lofts, ellipsoids, leaf blades; one skinned mesh per figure), HumanRig (15-bone player skeleton, shared with Animation), HumanFigure and SheepFigure in three styles (A Carved, B Soft, C Natural), and `Showcase.tscn`, a standalone scene with no server. Local check: dotnet build green; renders checked with Godot under xvfb + lavapipe (Vulkan). Render requested 65b8100 (style options).

- 2026-10-16 18:25: rig contract with Animation (session_019roNRWQBPw4ozNLDhzseig, claude/anim-1-player-animation): their bone names and tree (Hips, Spine, Neck, Head, ShoulderL/ElbowL/HandL, ShoulderR/ElbowR/HandR, HipL/KneeL/FootL, HipR/KneeR/FootR; rest at joints, zero rotations, arms down). They proposed Node3D pivots; I proposed the same tree as Skeleton3D bones with rigid weights (1 draw call per player); Animation said yes 17:33 (option 1); sha 067ed1c sent to them for #100. HumanRig renamed to match; `PlayerModel.Build()` returns the Skeleton3D with a `ToolR` BoneAttachment3D on HandR (shaft along -Y, head toward +Z).

- 2026-10-16 18:35: PR #96 CI green on b3be674; green sha sent to the PM.

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
Wait for Justin's style pick via the PM; meanwhile tune style A (recommended) on claude/model-1-style and plan the drop-in for Avatar (with Animation) and Sheep.cs.
