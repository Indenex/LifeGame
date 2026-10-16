# survival-8: New arrival warmth
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_review
Branch: claude/survival-arrival (main after #119 merged)
PR: (opening now, with survival-9)

## Goal
D-115: a new character's first 30 minutes online, cold drains at half speed, so a winter start (seasons, D-099) isn't a race to a fire. Not after a death. Visible as a small marker: a sprout badge in a draining ring right after the warmth ring; when it ends, one note "You've found your feet. The cold bites harder now."

## Design
- Vitals gains ArrivalLeft (seconds left at AsOf) and ArrivalRate (-1 online, 0 offline/expired), both [Default(0f)] so existing players and the world need no wipe; only new characters start with Needs.ArrivalSeconds (1800). Snapshot advances it like the needs; death sets it to 0. The tick rewrites the row when the rate flips, so the cold returns to normal within one 2 s tick.
- Cold multiplier Needs.ArrivalChill = 0.5 in WithSurroundings.
- Client: ArrivalBadge.cs in NeedsHud's gauge row after warmth; SurvivalClient.UpdateArrival drives it and posts the end note (not when a death ended it).

## Progress log
- 2026-10-16: built server and client per design; look proposed to UX/UI (options A badge [building], B glow on warmth ring, C sprout by the stamina bar). Not compiled locally (no dotnet).

- 2026-10-16: UX/UI agreed look A (badge after the warmth ring, no number per D-107). It already fades with the needs row under panels (it sits in the same panel) and the end note goes through hud.Note.

- 2026-10-16: Atmosphere seasons (claude/atmosphere-16-seasons, c41b1e6): winter offset -16 C, so winter days sit near 0 C (cold ~1.9x) and nights about -8 C (2x, the cap). No code change needed: AirTemperature includes the season. Snow doesn't wet (Weather.Rain is liquid only; snowfall is Module.Snowfall). In winter the new-arrival half-speed cold matters all day, not just at night.

- 2026-10-16: D-134: falling snow wets at half rain's rate. Survival.Precipitation = Rain + Needs.SnowWetting (0.5) x snowfall, through a partial hook SnowfallAt(ctx, ref snow) that Atmosphere implements with Module.Snowfall. Order is one-way (CS0759): this PR merges first, then Atmosphere adds the body in WorldClock.cs; send them (session_01KvVF5vpkYEHTKNRpHC18WN) the PR number on open and on merge.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the survival-8/9 PR; green: send the sha to the PM and Integration, and the PR number to Atmosphere (snow hook).
