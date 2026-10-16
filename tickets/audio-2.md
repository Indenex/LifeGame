# audio-2: The new land and the seasons sound right (M1)
Lane: Audio
Owner: Audio session (session_01LeXCWTFVBQcCNVEDdhPek3, Opus)
Status: in_progress
Branch: claude/audio-2-land-seasons (2c, PR #189); claude/audio-2b-footsteps (2b)
PR: (small PRs, listed below as they open)

## Goal
Three small PRs, each with a SoundPreview clip for Justin via Render and the PM:
- **2c seasons:** the ambience follows the season (Atmosphere's `Seasons`, #173): winter is windier and colder-sounding with the woods gone quiet (no birds or insects, bare branches whistle instead of leaves); autumn leaves rustle dry and crisp; spring birdsong is busier; summer days have insects buzzing and evenings have more crickets.
- **2a water:** running water along env-2's new streams (#175), with gentle lapping at the ponds as now; the sound comes from where the water is.
- **2b footsteps by surface:** grass, dirt, sand at the banks, shallow water and snow crunch (snow cover from #173).

## Progress log
- 2026-10-16 20:35: PM assigned audio-2 (M1). Branch claude/audio-2-land-seasons from main c09f9b8 + audio-1 ticket commits. Read env-2 (shared/Land.cs: RiverDistance is private) and atmosphere-16 (shared/Seasons.cs: Season, Warmth, Autumn; Atmosphere.Year/Season/SnowCover/Snowfall on the client). Footstep and water sensing live in Atmosphere.cs (Footsteps(), WaterSound()). Asked Environment for a public stream-distance call and Atmosphere to let Audio own that sensing in a partial file under atmosphere/audio/.
- 2026-10-16 21:10: 2c built: SoundMix.Year/SnowCover, SeasonSound.Of(year) (cold, summer, leaves, dry, birds, chorus, insects, frogs); winter wind slightly stronger/higher, bare-twig whistle, dry autumn leaves, snow hushes grass, NightVoices insect factor (owls kept), new DayInsects (summer drone + bee flybys), frogs by season. SoundScape.Year/SnowCover setters. SoundPreview scenes spring/summer/autumn/winter(+night). Standing levels, 40 s each, no music: spring -32.3, summer -29.9, autumn -32.5, winter -29.7 (mostly wind), nights -33 to -37. Existing scenes unchanged (storm -19.3, peak -5.2). Client + SoundPreview build. Game hook (Atmosphere sets sound.Year/SnowCover) waits for #173.
- 2026-10-16 21:30: #189 opened for 2c (head 955028d), Render requested 955028d. 2b on branch claude/audio-2b-footsteps (stacked on #189): Ground.Sand (grainy shuffle, firmer when wet) and Ground.Snow (crunch of crackles over a soft press); shallow water keeps the existing splash. SoundPreview sand/shallows/snow scenes. Loudest 50 ms of steps over their beds: grass -33.2, earth -32.5, sand -32.6, shallows -31.1, snow -30.9 dBFS. Sensing (which surface is underfoot) waits on Atmosphere's answer about moving Footsteps().

## Open questions / blockers
- Environment: public `Land.StreamDistance(x, z)` (meters to the nearest stream line) for 2a.
- Atmosphere: OK to move Footsteps()/WaterSound() into atmosphere/audio/AtmosphereSound.cs (partial class Atmosphere), and to read Atmosphere.Year/SnowCover there?

## Next step
Open the 2c PR, request a Render clip of the season scenes, then hook Year/SnowCover once #173 merges.
