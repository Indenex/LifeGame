# atmosphere-16: Seasons
Lane: World atmosphere (visual); leads seasons (D-099)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-16-seasons (stacked on claude/atmosphere-13-forest-light / #136)
PR: (none yet)

## Goal
D-099: a seasons system led by Atmosphere through WorldClock. Seasons shift temperature and weather, winter brings snow, and plants and animals change by season. A fast cycle first for testing; later a real-time multiplier, maybe tied to the real-world calendar. Partners: Environment (session_012hVejTxLoxBpZyX3GQmNL9) does snow cover and foliage; Animals (session_01SAcVBUUfPFFk1rdXuiQZRU) does seasonal fauna; Survival (session_014pHdaxJ7uESzsvWekpjvsH) does cold, capped at 2x combined chill. Start after atmosphere-13.

## Plan (approved by Justin, D-114: one in-game day per season, cold and snow first, before atmosphere-14/15)
- Clock: a new public table SeasonClock {Id, YearStart, DaysPerSeason} in WorldClock.cs (a new table, not new columns on WorldClock, so the no-wipe upgrade keeps working). Shared pure functions in a new shared/Seasons.cs: YearFraction(now, yearStart, daysPerSeason, dayLength), Season (Spring, Summer, Autumn, Winter), Warmth (-1 deep winter to 1 high summer, a smooth cosine peaking mid-summer), and SnowLine helpers. Admin reducers /season <name> and /season-speed <days per season>, like /time.
- Server: AirTemperature gains a seasonal offset (about +8 C in summer, -12 C in winter, so winter nights fall to about -6 C). NextWeather weights fronts by season (wetter spring and autumn, drier summer, snowier winter). Rain falls as snow below about 0.5 C; a public SeasonState row {Snow (0-1 falling now), SnowCover (0-1 on the ground, builds while snowing, melts above freezing, faster in sun)} is updated each WeatherTick.
- API for partners: Module.SeasonNow(ctx) -> (YearFraction, Season, Warmth), Module.SnowCover(ctx); client globals season_warmth, season_autumn (0-1, how far leaves have turned), snow_cover and snowing, set by Atmosphere every frame. Environment's foliage and ground shaders read them; Animals call SeasonNow; Survival keeps using AirTemperature (now seasonal) under its 2x chill cap.
- Atmosphere's own share: snowfall particles (Rain.cs grows a snow mode), winter light (lower, paler sun, bluer shadows, shorter-looking days), summer haze, autumn golden light; sky tint by season.

## Progress log
- 2026-10-16: queued (D-099); plan and options sent to the PM.

- 2026-10-16 18:17: Survival agrees to fold the seasonal offset into AirTemperature; their cold maxes at 2x at 0 C and below (cold x wet capped 2x), so -6 C nights are no harsher than 0 C (about 8 min without fire). They'll raise winter newcomer spawns with the PM (D-081). Snow doesn't wet players for now.

- 2026-10-16 18:20: Environment: all plants use my Look materials (Grass, Foliage, TreeFoliage/TreeBark), so snow and leaf colour go in my shaders. To do here: add Look.TreeLeaves (broadleaf variant of TreeFoliage, autumn colour; GhostOf covers it) and tell Environment; key seasonal shrink/fade on INSTANCE_CUSTOM.w (0 none, 1 broadleaf canopy: thins in winter and turns in autumn, 2 flower: spring and summer only; x/y/z stay the clearing animation). No snow line until env-2.

- 2026-10-16 18:26: Animals: SeasonNow + SnowCover are enough (spring lambing, winter fleece, huddling in snow, checked in their flock tick). Ping Animals (session_01SAcVBUUfPFFk1rdXuiQZRU) with the branch/sha once the API is on a branch, and when the PM picks the cycle length.

- 2026-10-16 18:33: D-114, all A: DaysPerSeason = 1 (a year is 4 days, 6 real hours); temperature and snow first; seasons before 14 and 15. Started branch claude/atmosphere-16-seasons on top of #136.

- 2026-10-16 18:45: pushed c41b1e6. Server (WorldClock.cs): SeasonClock {YearStart, DaysPerSeason=1} and SeasonState {Precipitation, Snow, SnowCover, Temperature}, both new public tables; precipitation eases as Weather.Rain used to and splits by Seasons.SnowFraction(temperature) into Weather.Rain (liquid, so Survival, ceramics drying and client rain are unchanged) and SeasonState.Snow; snow cover builds about 7 min in heavy snow, melts above 0.5 C. AirTemperature + Seasons.TemperatureOffset: +8 summer, -16 winter (raised from -12 after testing: at -12 a winter midday was 5.6 C and it only snowed at night; now 1.6 C and snows under cloud, nights about -8). Seasonal front odds. Admin reducers SetSeason, SetDaysPerSeason; Module.SeasonNow, SnowCover, Snowfall. Client: AtmosphereSeasons.cs (globals season_warmth, season_autumn, snow_cover, snowing; --season, --snow, --snow-cover args), Look shaders: snow on upward faces, autumn broadleaf colours and winter leaf drop on INSTANCE_CUSTOM.w == 1 foliage, flowers (w == 2) only spring/summer, grass straw in cold and snow-pressed, Rain.cs snow mode, paler winter sun and greyer saturation, snow brightens ambient. /season and /season-speed chat commands (KEYMAP.md). Checks: upgrade-check over origin/main passed; LoadTest 5x5, BuildTest, SpawnCheck, CraftCheck passed locally; set_season 0.875 -> 1.6 C at midday; set_days_per_season keeps the point in the year. Shaders not compiled yet (no Godot here); render requested.

- 2026-10-16 18:52: Survival wired D-134 through a partial hook declared in Survival.cs (claude/survival-arrival, 465a09b): `static partial void SnowfallAt(ReducerContext ctx, ref float snow);`. Add the body in WorldClock.cs ONLY once their declaration is on main (CS0759 otherwise): `static partial void SnowfallAt(ReducerContext ctx, ref float snow) => snow = Snowfall(ctx);`.

- 2026-10-16 19:00: D-136 (Justin): milder winter days, harsh cold at night. TemperatureOffset(warmth, daylight): summer +8; deep winter -5 by day, -17 at night. Measured locally (set_season 0.875): noon, cloud 0.2 -> 12.6 C (Survival chill about 1.16x); night 0.95 -> -8.6 C (capped 2x). Overcast winter day about 10 C (1.33x), rain and wind about 6 C (1.6x). Snow now falls mostly at night, so melt slowed to 0.0006 x min(T,10) x (1 + daylight + 2 rain) per tick: night snow lasts into the afternoon. Agreed with Survival (sent).

## Open questions
- (answered) D-134: falling snow wets you at half rain's rate; Survival wires it from Module.Snowfall (told them). Justin is fine with the -16 C winter. / blockers

## Next step
Wait for the Render lane's shots of c41b1e6 (and any shader errors); open the PR after #136 merges.
