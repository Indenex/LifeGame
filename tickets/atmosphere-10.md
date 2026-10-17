# atmosphere-10: Water and wind feel alive
Lane: World atmosphere and sound
Owner: Atmosphere thread
Status: in_review
Branch: claude/atmosphere-sound-hlqfke (local atmosphere-10-wip)
PR: #66 (with atmosphere-11)

## Goal
From the PM, 2026-10-16 (D-036 immersion): wherever there is water (ponds below Look.WaterLevel, which is also where Survival's drinking works) it gets a gentle shimmer, soft lapping by distance, rain rings on its surface while it rains, and frogs near water at night. Wind: trees sway and grass waves in slow gusts whose strength follows the weather, matched to the wind sound, so sight and sound move together. All crossfaded and subtle; no shiny ground. Done means screenshots of a pond (calm, windy, rain, night) and a short clip sent to the PM, and a PR green.

## Progress log
- 2026-10-16 03:40: started from main aaaf87a. Water is one plane at Look.WaterLevel following the player (Main.cs), so "water" = any cell below that height; there are no streams yet, so no babbling, only lapping. Added Look.WaterSurface (shader: slow wind ripples stirred by gusts, rain rings, depth fade at the bank, roughness 0.32, no mirror) hooked into Main's water plane. Globals wind_gust (WindVoice's gust, read from SoundScape.Gust) and rain_amount. Grass bend and tree sway now scale with wind_gust. SoundMix: WaterVoice (lapping sets, bank gulps, rain plinks), Frogs (4 voices, night * water). Atmosphere.WaterSound samples terrain every 2 m out to 20 m, 4x a second, for distance, amount and pan.
- 2026-10-16 04:00: ponds in this world are only ~10 cm deep (deepest within 80 m of spawn is -1.13 vs water line -1.0), so the old 0.6 m proximity fade left them nearly invisible; water now stays 85% opaque in the shallows with a 4 cm soft rim, shallow teal to deep teal, a soft sky tint at grazing angles, specular 0.12. Rain ring crests and steeper ripples catch a little sky so rings show without shine. Lapping raised to ~6 dB under the ambience. Screenshots /mnt/project-files/screenshots/atmosphere-18-pond-{calm,windy,rain,rain-closeup,night}.png; clip /mnt/project-files/audio/atmosphere-08-pond-approach-rain-night.mp3 (walk up to a pond, stand at it, steady rain on it, night with frogs). Pond near spawn: --debug-position=-17,0.
- 2026-10-16 04:15: PM added footsteps (Bug fixes measured grass steps at -22 dBFS over a -45 dBFS dawn bed). SoundMix now meters the ambience (smoothed RMS, 1.5 s) and scales steps by StepGain = clamp(0.12 + 9*rms, 0.12, 1): about -15 dB on a still dawn, full in a storm. Bare-earth steps softened ~4 dB. SoundPreview walking peaks: dawn -10.4 -> -20.2, midday -11.0 -> -22.7, hut -9.5 -> -16.7, steady rain -10.3 -> -12.4 dBFS. Clips: audio/atmosphere-09-footsteps-before.mp3 and -follow-ambience.mp3 (dawn, midday, steady rain, hut, 16 s each, walking the first half).
- 2026-10-16 04:40: merged main (crafting #62). Compatibility draw calls measured online (local server, db perf, --render-stats=1; the flag needs =1): 2,198 draw calls, 2,379 objects. Visible mesh nodes: BerryBush 92 bushes / 2,967 pieces, Terrain 302 (all trees, grass, rocks), Herd 160, GroundItems 47. Bushes are >= ~85% of draw calls; sent the PM a MultiMesh plan for BerryBush (Survival's file).
- 2026-10-16 05:30: PM added: player hidden under dense canopy (playtest2-hidden-under-canopy.png). Tree fade now reaches 1.2-2.6 tree sizes from the camera-player line (was 0.9-1.9; max reach 4.9 m, inside Terrain.ShowGhosts' 6 m margin) and the window around the player is fully clear (alpha 1 - fade*(0.35+0.65*window), window 0.09-0.3 on screen). Shots: screenshots/atmosphere-20-canopy-{before,after,after-turned}.png at --debug-position=-44,-46 (densest woods near spawn).

## Open questions / blockers
- (none)

## Next step
Wait for the PR's CI and Justin's view of the pond screenshots via the PM; adjust the water look if he asks.
