# atmosphere-19: Warm homes at night
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-19-warm-homes
PR: (none yet)

## Goal
Justin's pick (2026-10-16, via the PM), order 19 warm homes, 20 wet world, 21 storms. At night, camps look lived-in from afar: warm firelight glows from hut doorways and gaps in walls, and smoke rises from hearths and chimneys and drifts with the wind. Lights and smoke follow a hearth or fire inside or by a structure; strength follows Atmosphere.Night and the fire's fuel. Coordinate with House building (which owns huts, doors and structure pieces) on where doorways, gaps and hearths are; the glow and smoke are Atmosphere's. Done: Render lane shots of a hut with a fire at dusk and night from 10 m and 40 m, and a PR green.

## Progress log
- 2026-10-16 21:45: ticket opened.

- 2026-10-16 21:50: first build 682361f (client builds). New AtmosphereHomes.cs: once a second, each burning fire (Fire rows) within 90 m is matched to a finished structure whose shape has a doorway (Structures.HasDoorway / Contains, data-driven, no per-kind code); up to 6 homes get a warm OmniLight just outside the doorway (Structures.ToWorld, DoorwayZ + 0.15) and an additive glow quad in the doorway plane, both x (0.25 + 0.75 Night) and the fire's fuel strength, and about 0.2 when the structure_door row says Closed. Up to 8 smoke plumes: from home roofs (2.9 m, a local default until House building adds a smoke-outlet height to StructureShape, per D-206) and from open fires (0.9 m, thinner), drifting with WindDirection and the wind. Render requested 682361f. Merged main (with #275 and D-206).

## Open questions / blockers
- Answered from public data: doorway from StructureShape.DoorwayZ, door state from structure_door. Asked House building (session_01DD9p6EhSc5pJ8CjNmDFTrG) for a smoke-outlet height on StructureShape (D-206) and checking the glow with see-through buildings (#272).

## Next step
Open the atmosphere-19 PR; switch the 2.9 m roof default to House building's shape field once it exists.
