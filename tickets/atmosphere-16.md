# atmosphere-16: Seasons
Lane: World atmosphere (visual); leads seasons (D-099)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: todo
Branch: (set when started)
PR: (none yet)

## Goal
D-099: a seasons system led by Atmosphere through WorldClock. Seasons shift temperature and weather, winter brings snow, and plants and animals change by season. A fast cycle first for testing; later a real-time multiplier, maybe tied to the real-world calendar. Partners: Environment (session_012hVejTxLoxBpZyX3GQmNL9) does snow cover and foliage; Animals (session_01SAcVBUUfPFFk1rdXuiQZRU) does seasonal fauna; Survival (session_014pHdaxJ7uESzsvWekpjvsH) does cold, capped at 2x combined chill. Start after atmosphere-13.

## Plan (sent to the PM 2026-10-16; options pending)
- Clock: a new public table SeasonClock {Id, YearStart, DaysPerSeason} in WorldClock.cs (a new table, not new columns on WorldClock, so the no-wipe upgrade keeps working). Shared pure functions in a new shared/Seasons.cs: YearFraction(now, yearStart, daysPerSeason, dayLength), Season (Spring, Summer, Autumn, Winter), Warmth (-1 deep winter to 1 high summer, a smooth cosine peaking mid-summer), and SnowLine helpers. Admin reducers /season <name> and /season-speed <days per season>, like /time.
- Server: AirTemperature gains a seasonal offset (about +8 C in summer, -12 C in winter, so winter nights fall to about -6 C). NextWeather weights fronts by season (wetter spring and autumn, drier summer, snowier winter). Rain falls as snow below about 0.5 C; a public SeasonState row {Snow (0-1 falling now), SnowCover (0-1 on the ground, builds while snowing, melts above freezing, faster in sun)} is updated each WeatherTick.
- API for partners: Module.SeasonNow(ctx) -> (YearFraction, Season, Warmth), Module.SnowCover(ctx); client globals season_warmth, season_autumn (0-1, how far leaves have turned), snow_cover and snowing, set by Atmosphere every frame. Environment's foliage and ground shaders read them; Animals call SeasonNow; Survival keeps using AirTemperature (now seasonal) under its 2x chill cap.
- Atmosphere's own share: snowfall particles (Rain.cs grows a snow mode), winter light (lower, paler sun, bluer shadows, shorter-looking days), summer haze, autumn golden light; sky tint by season.

## Progress log
- 2026-10-16: queued (D-099); plan and options sent to the PM.

- 2026-10-16 18:17: Survival agrees to fold the seasonal offset into AirTemperature; their cold maxes at 2x at 0 C and below (cold x wet capped 2x), so -6 C nights are no harsher than 0 C (about 8 min without fire). They'll raise winter newcomer spawns with the PM (D-081). Snow doesn't wet players for now.

## Open questions
- For Justin later (with the seasons render): should falling snow wet you? Options: no (Survival's current default); yes, as half of rain's rate (recommended, so snow feels cold but not drenching); same as rain. / blockers
- PM/Justin: test cycle length and what changes first (options sent).

## Next step
After atmosphere-13 is in review and the PM answers: create the branch, add shared/Seasons.cs and the SeasonClock table.
