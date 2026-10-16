# survival-11: Breath in cold air
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_progress
Branch: claude/survival-next
PR: (none yet)

## Goal
D-144: in cold air every player's breath shows as a soft white puff every few seconds (no camera shake). Denser and longer-lasting the colder it is; none above about 6 C. Remote players too.

## Design (proposed to Atmosphere, session_01KvVF5vpkYEHTKNRpHC18WN)
- client/scripts/survival/BreathPuff.cs: GPUParticles3D at the mouth, unshaded blue-white, alpha ~0.35, drifts up and fades ~1 s, one puff per ~3 s.
- Needs the air temperature on the client: asked Atmosphere whether they can expose Atmosphere.AirCelsius (same formula as the server), or else a coarse AirC on a public server row.

## Progress log
- 2026-10-16: assigned (D-144). Proposal sent to Atmosphere.

## Open questions / blockers
- Client air temperature source (Atmosphere).

## Next step
When Atmosphere answers, build BreathPuff.cs and hook it on the local player and remote avatars (SurvivalClient.AvatarOf).
