# atmosphere-27: Other players in the water, seen
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-27-others-water
PR: #440

## Goal
Follow-up to atmosphere-26 (#429), which gave only your own player ripples, splash and wet legs. Now other players wading or floating within 60 m get the same painted ripple rings and a splash when they step in (up to 12 at once). Their legs stay dry, because the wet band is a single global that follows you (option (a) sent to the PM; (b) wet legs for everyone would need a per-figure shader value). Client-only. Done: PR green and merged.

## Progress log
- 2026-10-17 02:35: `AtmosphereWater.cs` gains a per-figure `OtherWater` pool, and Main gets a one-line hook (`atmosphere.OtherFigures = () => others.Values;`). The client builds with 0 errors.

## Open questions / blockers
- Waiting on Justin via the PM: (a) rings and splash only (built), (b) wet legs too, or (c) none.

## Next step
Wait for #440 CI and send the PM the green sha.
