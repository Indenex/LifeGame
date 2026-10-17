# building-16: Hen coop
Lane: House building
Owner: House building thread (Sonnet)
Status: in_review
Branch: claude/house-building-8ueqz2-coop
PR: (see STATUS)

## Goal
A small claim-only hen coop (Justin picked option 1; Animals' hens #454 roost in it at night). Players build it in three stages inside their claim: a shed on legs with woven walls, a pop-hole and ramp at the front, and a thatched roof. Animals targets two points per coop, both in the shape data: `Entry` (where a hen walks to go in, at the foot of the ramp) and `Roost` (where it settles inside), as local offsets scaled with the shape.

## Progress log
- 2026-10-17: `StructureKind.HenCoop` appended after TiledHouse (re-append if another kind lands first). One Shapes entry (Category "Animals", not Wild so claim-only, 1.8 x 1.4 m, Clearance 1.5, RainCover 1, Warmth 0.2, `Entry = (0, 1.5)`, `Roost = (0, -0.1)`), costs Stick 5 / Stick 4 + Fiber 4 / Fiber 6 (a little less than a lean-to). `Model = "Coop"` plus `StructureModel.Coop.cs` (rough prototype, from the existing stick/weave/thatch kit). StageProcess: frame, wattle, roof.
- Tests: BuildTest `coop` mode (and `both`) checks the shared numbers and builds it with two players inside a claim; passes locally (53 s). Upgrade check passes. Server and client build.
- Not seen in Godot yet; a render request will go to the Render lane. The model is a rough prototype until Justin picks the animal/structure look (D-154 does not cover a hen coop beyond that).

## Open questions / blockers
- Stacked on #445 (building-12 step 2: the `Model` field). Merge #445 first.
- Animals: read `Structures.Shape(kind).Entry` / `.Roost` with `Structures.ToWorld(sx, sz, yaw, x, z)` for the world points; the coop is not solid and has no door, so nothing blocks hens walking in.

## Next step
Wait for CI and send the PM the green sha; then answer any Animals questions about Entry/Roost.
