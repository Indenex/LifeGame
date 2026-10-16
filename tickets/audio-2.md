# audio-2: The new land and the seasons sound right (M1)
Lane: Audio
Owner: Audio session (session_01LeXCWTFVBQcCNVEDdhPek3, Opus)
Status: in_progress
Branch: claude/audio-2-land-seasons
PR: (small PRs, listed below as they open)

## Goal
Three small PRs, each with a SoundPreview clip for Justin via Render and the PM:
- **2c seasons:** the ambience follows the season (Atmosphere's `Seasons`, #173): winter is windier and colder-sounding with the woods gone quiet (no birds or insects, bare branches whistle instead of leaves); autumn leaves rustle dry and crisp; spring birdsong is busier; summer days have insects buzzing and evenings have more crickets.
- **2a water:** running water along env-2's new streams (#175), with gentle lapping at the ponds as now; the sound comes from where the water is.
- **2b footsteps by surface:** grass, dirt, sand at the banks, shallow water and snow crunch (snow cover from #173).

## Progress log
- 2026-10-16 20:35: PM assigned audio-2 (M1). Branch claude/audio-2-land-seasons from main c09f9b8 + audio-1 ticket commits. Read env-2 (shared/Land.cs: RiverDistance is private) and atmosphere-16 (shared/Seasons.cs: Season, Warmth, Autumn; Atmosphere.Year/Season/SnowCover/Snowfall on the client). Footstep and water sensing live in Atmosphere.cs (Footsteps(), WaterSound()). Asked Environment for a public stream-distance call and Atmosphere to let Audio own that sensing in a partial file under atmosphere/audio/.

## Open questions / blockers
- Environment: public `Land.StreamDistance(x, z)` (meters to the nearest stream line) for 2a.
- Atmosphere: OK to move Footsteps()/WaterSound() into atmosphere/audio/AtmosphereSound.cs (partial class Atmosphere), and to read Atmosphere.Year/SnowCover there?

## Next step
Build 2c in SoundMix (Season inputs: SeasonWarmth, Autumn, SnowCover) with SoundPreview scenes spring/summer/autumn/winter.
