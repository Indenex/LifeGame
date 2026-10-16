# atmosphere-22: Distant land melts into the fog
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_review
Branch: claude/atmosphere-farland-fog
PR: #289

## Goal
Environment's env-9 "seeing further" (Justin's pick; branch claude/environment-farland, client/scripts/environment/FarLand.cs) draws coarse ground and tree silhouettes beyond the loaded chunks, out to 128 m (96 on Near, off on Off). At the normal fog density (0.005-0.009) the 128 m rim is only 50-70% fogged, so on a clear day the edge of the disc could show against the sky. Done: the rim melts fully into the fog colour, with no seam with the near land, and a PR green.

## Progress log
- 2026-10-16 22:10: Environment proposed option A (a far_land_radius global plus a fade in Look's shaders); Atmosphere took it.
- 2026-10-16 22:15: first build (client 0 errors).
  - Look.EnsureWeatherGlobals registers `far_land_radius` (float, default 0; FarLand sets it) and `fog_tint` (colour; Atmosphere sets it to env.FogLightColor every frame).
  - Painted shader (ground, bark, rock, foliage and their see-through variant): over the outer quarter of the radius, measured flat from player_position, ALBEDO fades to 0 and EMISSION to fog_tint. Godot's own fog then finishes the job, so the rim is fog-coloured at any density. Inside 0.75 x radius nothing changes, so there is no seam with the near land.

## Open questions / blockers
- (none) Environment sets `far_land_radius` from FarLand once this is on main.

## Next step
Wait for PR CI and send the PM the green sha.
