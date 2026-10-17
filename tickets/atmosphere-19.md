# atmosphere-19: Warm homes at night
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_review
Branch: claude/atmosphere-19b-homes (follow-up; first part claude/atmosphere-19-warm-homes, #281 merged)
PR: #281 (merged), follow-up #407

## Goal
Justin's pick (2026-10-16, via the PM), order 19 warm homes, 20 wet world, 21 storms. At night, camps look lived-in from afar: warm firelight glows from hut doorways and gaps in walls, and smoke rises from hearths and chimneys and drifts with the wind. Lights and smoke follow a hearth or fire inside or by a structure; strength follows Atmosphere.Night and the fire's fuel. Coordinate with House building (which owns huts, doors and structure pieces) on where doorways, gaps and hearths are; the glow and smoke are Atmosphere's. Done: Render lane shots of a hut with a fire at dusk and night from 10 m and 40 m, and a PR green.

## Progress log
- 2026-10-16 21:45: ticket opened.

- 2026-10-16 21:50: first build 682361f (client builds). New AtmosphereHomes.cs: once a second, each burning fire (Fire rows) within 90 m is matched to a finished structure whose shape has a doorway (Structures.HasDoorway / Contains, data-driven, no per-kind code); up to 6 homes get a warm OmniLight just outside the doorway (Structures.ToWorld, DoorwayZ + 0.15) and an additive glow quad in the doorway plane, both x (0.25 + 0.75 Night) and the fire's fuel strength, and about 0.2 when the structure_door row says Closed. Up to 8 smoke plumes: from home roofs (2.9 m, a local default until House building adds a smoke-outlet height to StructureShape, per D-206) and from open fires (0.9 m, thinner), drifting with WindDirection and the wind. Render requested 682361f. Merged main (with #275 and D-206).

- 2026-10-16 22:10: House building answered. `StructureShape.SmokeY` arrives in PR #271 (clay hut 2.9, mud-brick house 3.3, log cabin 3.3, others 0). `BuildingSystem.SeeThroughOf(ulong structureId)` (0..1) arrives in PR #272, after #271. Plan: smoke at SmokeY, and none when it is 0. Doorway glow quad alpha and door light energy are multiplied by (1 - SeeThroughOf(id)). Smoke does not fade.
- 2026-10-16 22:09: CI run 38972227238 green on 5702699; PR #281 merged. Follow-up still open: SmokeY and SeeThroughOf from House building.
- 2026-10-17 01:15: follow-up, now that House building's #271 (SmokeY) and #272 (see-through shelters) are on main:
  - Roof smoke rises from `ground + StructureShape.SmokeY` (clay hut 2.9, mud-brick house and log cabin 3.3). Shapes with SmokeY 0 send up no smoke. This is data-driven per D-206 and replaces the flat 2.9 m default.
  - Door light energy and the doorway glow quad are multiplied by (1 - BuildingSystem.SeeThroughOf(id)), so a home faded out of the camera's way doesn't leave its glow hanging in the air. Smoke does not fade.
  - Hook: `Atmosphere.SeeThrough` (Func<ulong, float>), set in Main with one line next to the existing Sheltered hook.
  - Client 0 errors.

## Open questions / blockers
- (none)

## Next step
Wait for #407 CI and send the PM the green sha.
