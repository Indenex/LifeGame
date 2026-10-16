# atmosphere-10: Water and wind feel alive
Lane: World atmosphere and sound
Owner: Atmosphere thread
Status: in_progress
Branch: claude/atmosphere-sound-hlqfke (local atmosphere-10-wip)
PR: (none yet)

## Goal
From the PM, 2026-10-16 (D-036 immersion): wherever there is water (ponds below Look.WaterLevel, which is also where Survival's drinking works) it gets a gentle shimmer, soft lapping by distance, rain rings on its surface while it rains, and frogs near water at night. Wind: trees sway and grass waves in slow gusts whose strength follows the weather, matched to the wind sound, so sight and sound move together. All crossfaded and subtle; no shiny ground. Done means screenshots of a pond (calm, windy, rain, night) and a short clip sent to the PM, and a PR green.

## Progress log
- 2026-10-16 03:40: started from main aaaf87a. Water is one plane at Look.WaterLevel following the player (Main.cs), so "water" = any cell below that height; there are no streams yet, so no babbling, only lapping. Added Look.WaterSurface (shader: slow wind ripples stirred by gusts, rain rings, depth fade at the bank, roughness 0.32, no mirror) hooked into Main's water plane. Globals wind_gust (WindVoice's gust, read from SoundScape.Gust) and rain_amount. Grass bend and tree sway now scale with wind_gust. SoundMix: WaterVoice (lapping sets, bank gulps, rain plinks), Frogs (4 voices, night * water). Atmosphere.WaterSound samples terrain every 2 m out to 20 m, 4x a second, for distance, amount and pan.

## Open questions / blockers
- (none)

## Next step
Add pond scenes to tools/SoundPreview and render a clip; render pond screenshots (find a pond cell near spawn, use --debug-position).
