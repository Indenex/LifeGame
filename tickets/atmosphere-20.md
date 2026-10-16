# atmosphere-20: Wet world after rain
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-20-wet-world
PR: #282

## Goal
Justin's pick (2026-10-16, via the PM), order 19 warm homes, 20 wet world, 21 storms. After rain, ground and stones darken and take a soft sheen (the shaders already darken with the `wetness` global; this adds a gentle shine where it should and keeps it subtle), and puddles form in hollows of the land, then slowly shrink and dry. Puddles come from the server's Weather.Wetness plus local low ground (World.BaseHeight), drawn as flat reflective patches in Look's ground shader or as decals. Coordinate with Environment for ground cover and paths. Done: Render lane shots of a meadow and a path during rain, just after, and an hour later, and a PR green.

## Progress log
- 2026-10-16 21:45: ticket opened.

- 2026-10-16 22:00: started from main while #281 runs CI. New global `puddles` (0 to 1): smoothstep(0.45, 0.9, wetness), none on snowy ground, eased over 20 s; --puddles=1 holds it. Look painted shader, ground material only (puddles_ok = 1 via WithPuddles): puddles on flat ground (normal.y 0.93 to 0.99) in the low spots of the detail noise at 0.035 scale, covering up to about 42% as puddles goes to 1; darker (x0.5), roughness 0.05, specular 0.5, normal flattened to the water plane, sun glint via wet. Wet darkening and sheen elsewhere unchanged. Client builds.

## Open questions / blockers
- (none yet)

## Next step
Wait for #282 CI and send the PM the green sha; then start atmosphere-21 (storms).
