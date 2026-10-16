# atmosphere-20: Wet world after rain
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: todo
Branch: (set when started)
PR: (none yet)

## Goal
Justin's pick (2026-10-16, via the PM), order 19 warm homes, 20 wet world, 21 storms. After rain, ground and stones darken and take a soft sheen (the shaders already darken with the `wetness` global; this adds a gentle shine where it should and keeps it subtle), and puddles form in hollows of the land, then slowly shrink and dry. Puddles come from the server's Weather.Wetness plus local low ground (World.BaseHeight), drawn as flat reflective patches in Look's ground shader or as decals. Coordinate with Environment for ground cover and paths. Done: Render lane shots of a meadow and a path during rain, just after, and an hour later, and a PR green.

## Progress log
- 2026-10-16 21:45: ticket opened.

## Open questions / blockers
- (none yet)

## Next step
Start after atmosphere-19 is in review.
