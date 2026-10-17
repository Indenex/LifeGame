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

- 2026-10-17 06:20: Render B's stills (renders/atmosphere-28-7c8b799-1..7) show the shader draws, but full frost read as light snow, tinted pink by dawn. Lighter version: rime only on upward faces and the raised grain (relief h 0.35 to 0.8), at 30% strength on bare ground and 55% on rocks and leaves, about 35 to 45% of before. The colour is a cooler blue-white (0.74, 0.84, 0.97); grass silvers only at the tips (top 55%, at most 55%). The half-melt pattern is unchanged. The dark, see-through foreground pine in shots 3 and 5 is the trees' camera fade (it stands between the camera and the player), as designed.

- 2026-10-17 06:35: Justin answered via the PM.
  - (1) Autumn only: frost needs Year between about 0.5 and 0.75.
  - (2) Some mornings only, by local cold, the same for every player. Each night (counted toward the morning it ends in) rolls World.Noise(night, 0, 1, 977): frost on a little under half of autumn mornings, with its own strength (0.55 to 1). The cold threshold is now under about 8 °C, so autumn nights can frost. In the shaders, `frost_local()` makes higher ground and damp ground by the water frost more, in broad patches (about 250 m) that shift each morning through the `frost_seed` global.
  - (3) Lighter, as in 864924de.
  - (4) He likes the heavy white look for snow: follow-up ticket atmosphere-33.

- 2026-10-17 06:55: Render B's re-shoot showed the lighter frost nearly invisible. Justin picked "in between" (follow-up branch claude/atmosphere-28b-frost-strength).
  - Grass tips are clearly silvered (0.8 strength over the top 65% of the blade).
  - Rocks and leaves get a white edge on upper faces and raised grain (normal.y 0.1 to 0.6, relief 0.25 to 0.65, strength 0.75).
  - Flat bare ground keeps a trace (0.2).
  - The local-cold factor now only halves frost at most, so a frosty morning is visible everywhere.

## Open questions / blockers
- (none) Justin picked (a) morning frost (via the PM, 2026-10-17 03:02).

## Next step
Open the PR, ask Render B for the same 7 spots, and send the PM the green sha.
