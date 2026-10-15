# atmosphere-1: Day and night, weather, lighting and sound
Lane: World atmosphere and sound
Owner: World atmosphere and sound thread
Status: in_review
Branch: claude/atmosphere-sound-hlqfke
PR: #9

## Goal
Day and night (about 90 real minutes per in-game day, synced from the server), rain other systems can read, lighting polish, ambient sound, footsteps and gentle music, in the Valheim-like direction. Done when a full day and night with rain plays smoothly in a build and Justin has seen screenshots.

## Progress log
- 2026-10-15: lane started (ticket created by the PM from the lane brief)
- 2026-10-15 21:20: first pass committed. Server: `world_clock` and `weather` tables, weather tick every 20 s, helpers `TimeOfDay`, `CurrentWeather`, `DryingRate` (halved in rain, per the pot-drying decision), `AirTemperature`. Client: `client/scripts/atmosphere/` (sun/moon path, keyframed sky and fog, custom sky shader with clouds and stars, cloud shadows and wet ground in `Look.cs`, rain, fireflies) and a synthesized soundscape (wind, rain, birds, crickets, owl, footsteps, generative music). Preview keys T (fast-forward), R (weather), M (music). `tools/SoundPreview` renders the audio to a WAV. Hooks: 3 lines in `Main.cs`, 2 calls in `Reducers.cs`.
- Upgrading an existing database works without `--delete-data` (tables are created on next connect); tested locally.

- 2026-10-15 21:42: merged main (survival), preview keys moved to F5/F6 debug-only (D-025). CI run https://github.com/Indenex/LifeGame/actions/runs/38811460203 green on a6b8239. Asked PM for merge.
- Queued for atmosphere-2 (PM): sheep bleats driven by Animals state changes; canopy fade between camera and player (foliage shader dither in Look.cs). Ceramics RainAt one-liner goes in whichever PR merges second.

## Open questions / blockers
- (none)

## Next step
Wait for Justin's merge of PR #9 via the PM; then start atmosphere-2.
