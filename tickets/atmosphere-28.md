# atmosphere-28: Morning frost
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-28-frost
PR: #459

## Goal
Option (a), sent to the PM for Justin on 2026-10-17 (recommended: morning frost; the alternatives were (b) motes in sunbeams and (c) heat shimmer). After a cold, clear, still night (air below about 3 °C), a pale rime settles on the ground, grass tips, leaves, rocks and bark, with small glints that sparkle as you move. Once the sun is up it melts back in patches, gone by mid-morning; warm air and rain melt it faster, and overcast nights bring none. People, animals and made things (Look.Solid, no cavity) stay clear. Client-only. Done: PR green, and Render shots of a frosty dawn and of it half-melted.

## Progress log
- 2026-10-17 03:10: `AtmosphereFrost.cs` (new) sets the global `frost`. `Look.cs` registers `frost`; the painted shader whitens faces open to the sky in patches (from the detail texture) with glints, and the grass shader silvers the blade tips. Hooks in `Atmosphere.cs`: `ReadyFrost()` and `UpdateFrost(dt)`. `--frost=0..1` holds it for screenshots. The client builds with 0 errors.

- 2026-10-17 03:02: CI run 38997049144 green on bbec24b8. Justin picked (a). Render has 6 stills queued (meadow near spawn: frost 1, 0.45 and 0, each wide and close).

## Open questions / blockers
- (none) Justin picked (a) morning frost (via the PM, 2026-10-17 03:02).

## Next step
Wait for CI on this ticket push, then send the PM the green sha. Tune from Render's frost stills; the PM may merge once they confirm the shaders compile.
