# building-16: Hen coop
Lane: House building
Owner: House building thread (Sonnet)
Status: done
Branch: claude/house-building-8ueqz2-coop
PR: #479 (merged)

## Goal
A small claim-only hen coop (Justin picked option 1; Animals' hens #454 roost in it at night). Players build it in three stages inside their claim: a shed on legs with woven walls, a pop-hole and ramp at the front, and a thatched roof. Animals targets two points per coop, both in the shape data: `Entry` (where a hen walks to go in, at the foot of the ramp) and `Roost` (where it settles inside), as local offsets scaled with the shape.

## Progress log
- 2026-10-17: `StructureKind.HenCoop` appended after TiledHouse (re-append if another kind lands first). One Shapes entry (Category "Animals", not Wild so claim-only, 1.8 x 1.4 m, Clearance 1.5, RainCover 1, Warmth 0.2, `Entry = (0, 1.5)`, `Roost = (0, -0.1)`), costs Stick 5 / Stick 4 + Fiber 4 / Fiber 6 (a little less than a lean-to). `Model = "Coop"` plus `StructureModel.Coop.cs` (rough prototype, from the existing stick/weave/thatch kit). StageProcess: frame, wattle, roof.
- Tests: BuildTest `coop` mode (and `both`) checks the shared numbers and builds it with two players inside a claim; passes locally (53 s). Upgrade check passes. Server and client build.
- Not seen in Godot yet; a render request will go to the Render lane. The model is a rough prototype until Justin picks the animal/structure look (D-154 does not cover a hen coop beyond that).

- 2026-10-17: merged main with the StructureDef registry (#422). The def is in shared/structures/StructuresHouse.cs as `Kind = 18` (after ClayTrough = 17), and `HenCoop` is the StructureKind position after it. NOTE: a SpacetimeDB enum cannot carry explicit values (the build refuses `HenCoop = 100`: BSATN0002), so the 100+ House range cannot be used; a def's Kind has to equal its enum position. Raised with the PM/Integration.

- 2026-10-17: PM fixed the kind order (ClayTrough 17, HenCoop 18, StoneHearth 19, devices 20-22). This branch carries Ceramics' #417 (ClayTrough) merged in so HenCoop = 18 is right in isolation; #417 must merge first or alongside.

## Open questions / blockers
- Stacked on #445 (building-12 step 2: the `Model` field). Merge #445 first.
- Animals: read `Structures.Shape(kind).Entry` / `.Roost` with `Structures.ToWorld(sx, sz, yaw, x, z)` for the world points; the coop is not solid and has no door, so nothing blocks hens walking in.

## Next step
Nothing; merged.
