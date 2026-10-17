# atmosphere-24: Falling leaves and blossom
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: done
Branch: claude/atmosphere-24-falling-leaves
PR: #349

## Goal
Justin's pick (2026-10-16, via the PM): leaves drift down under broadleaf trees in autumn and petals fall in spring, more in wind. Keep it cheap: GPU particles, only near the player. Done: a Render lane autumn clip in woods, and a PR green.

## Progress log
- 2026-10-16 23:30: first build (server and client 0 errors).
  - Data-driven per docs/CONTENT.md. New shared/SeasonEvents.cs holds the `SeasonEventDef` registry. Entries: falling leaves (year 0.52 to 0.74, full 0.58 to 0.68, autumn golds, oranges, reds and browns, 11 cm) and blossom (0.05 to 0.21, full 0.09 to 0.16, white and pale pink, 6 cm, about half as many). Each entry has rate, wind boost and an under-trees flag. `SeasonEvents.Strength(def, year, wind, woods)` gives how much is falling.
  - New client/scripts/atmosphere/FallingLeaves.cs: one GpuParticles3D per entry (120 max, 12 s life), in a 28 m box from 8 to 14 m up round the player. Drag holds the fall to about 1 m/s, turbulence adds flutter, pieces spin, and each shrinks away at the end. Gravity leans with WindDirection, 0.3 + 2.2 x wind. Emission scales by AmountRatio; out of season it is off. Colours come from the def, lit by the scene.
  - Atmosphere.cs: adds the node and calls `leaves.Follow(focus, Year, wind, woods, WindDirection)` next to the woodedness easing. "Under trees" uses the same woodedness (Forest.Density around the player) as the sun shafts, so it is not species-aware yet.
  - `--leaves=1` holds every entry at full, for renders.
- 2026-10-16 23:40: CI run 38981101988 green on 6dac9f8; PR #349 merged.

## Open questions / blockers
- (none)

## Next step
Done. Tune size, count and fall speed from the Render autumn clip when it arrives.
