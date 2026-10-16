# atmosphere-23: Dawn valley mist that reads
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-tickets-close
PR: #347

## Goal
The first valley-mist renders (atmosphere-17, da2c2fb) were weak: the woods spot showed only a faint haze at dawn, and dusk was too dark to see any mist. Make dawn mist clearly visible in hollows and by water: denser, a little taller, and glowing warm when you look toward the low sun. Done: Render lane shots at the pond (30,76) at dawn show mist pooled in the low ground and lit toward the sun, and a PR green.

## Progress log
- 2026-10-16 23:20: first build (client 0 errors).
  - Mist.cs: three layers (0.5, 0.95, 1.5 m above the low ground; was 0.55, 0.9), so a hollow fills knee- to waist-deep.
  - Pooling now ramps over 1.8 m (was 1.4).
  - Opacity x0.55 (was 0.42), capped at 0.72 (was 0.6), with a stronger base under the wisps (0.2, was 0.15).
  - Forward scatter: looking toward the sun, the mist brightens by the sunlight and gains up to 35% opacity (pow 6 lobe).
  - Atmosphere passes the sun direction, plus a glow colour that only shows while the sun is low (sun height -0.02 to 0.5) and dims under cloud.
  - Mist.Amount (timing) is unchanged.

## Open questions / blockers
- Waiting for the Render lane's pond set (30,76) to judge it; the PM queued it.

## Next step
Wait for #347 CI and send the PM the green sha. Render request sent (pond 30,76 at dawn, toward and away from the sun). Tune from the shots.
