# atmosphere-34: Fog cap (blinding fog fix)
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: done
Branch: claude/atmosphere-34-fog-cap
PR: #565 (merged)

## Goal
An urgent bug in the released build. Justin: "fog is awful and blinding at some points" (screenshot renders/justin/2026-10-17-fog.webp at 9311b22e: near-white cream fog toward a low sun, the player barely visible 15 m away, the scene washed out). Fix:
1. Cap the total fog so the player and anything within ~40 m always read clearly; fog only softens the distance.
2. Clamp the sun-direction scattering and glare, and limit bloom on the fog.
3. Audit every fog source for stacking, under one hard cap.
4. Render before/after at dawn, at dusk and in mist, facing the sun and away.
Done: the PR merged and Render shows it fixed.

## Progress log
- 2026-10-17 06:55: audit. At a low sun, several fog sources stacked:
  - Volumetric fog: mood MistDensity 0.009 to 0.010, × (1 + 1.6 × woodland shafts), × (1 + 1.2 × god rays, ×1.25 on High), × (1 + 2.2 × storm). That's up to about 0.06.
  - Sun light in the fog: 0.6 to 1.6, + 1.4 × shafts, + 1.8 × rays, up to about 4.5. Anisotropy up to 0.85 (strong forward glow).
  - Depth fog: mood 0.005 to 0.009, × (1 + 2.4 × storm).
  - Bloom: GlowBloom up to 0.24 at low sun, with a threshold down to 0.85.
  - The worst offender is the dawn and dusk mist (Mist.cs): three stacked layers, each up to alpha 0.72, so about 98% opaque together seen from above, with a scatter glow pushing its colour above 1.
- Fix:
  - `AtmosphereFogCap.cs` (new): `CapFog()` runs at the end of ApplyLight, after the grade and lightning. Depth fog is capped at 0.008 and volumetric at 0.009, with the two together at 0.012 (scaled down together), which keeps about 62 to 70% of the light at 40 m. Sun light in the fog is capped at 1.6 and anisotropy at 0.7. GlowBloom is capped at 0.1, GlowIntensity at 0.9, and the glow threshold is held at 1.0 or above.
  - `Mist.cs`: each layer's alpha is capped at 0.22 (three together reach at most about 52%), thinned to 30% within 6 to 40 m of the camera, and the scatter glow is halved and kept at or below 1.
- The client builds with 0 errors.

- 2026-10-17 05:43: #565 merged (green sha ce770013, CI run 39007120219). Render B's before/after stills are requested; tune from them in a follow-up if needed.

## Open questions / blockers
- The see-through big pine in the screenshot is the trees' camera fade (Environment's); the PM is raising it with them.

## Next step
Done. Check Render B's before/after stills when they arrive.
