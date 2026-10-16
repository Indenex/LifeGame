# survival-8: New arrival warmth
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_progress
Branch: claude/survival-arrival (stacked on claude/survival-running, #119)
PR: (open after #119 merges)

## Goal
D-115: a new character's first 30 minutes online, cold drains at half speed, so a winter start (seasons, D-099) isn't a race to a fire. Not after a death. Visible as a small marker: a sprout badge in a draining ring right after the warmth ring; when it ends, one note "You've found your feet. The cold bites harder now."

## Design
- Vitals gains ArrivalLeft (seconds left at AsOf) and ArrivalRate (-1 online, 0 offline/expired), both [Default(0f)] so existing players and the world need no wipe; only new characters start with Needs.ArrivalSeconds (1800). Snapshot advances it like the needs; death sets it to 0. The tick rewrites the row when the rate flips, so the cold returns to normal within one 2 s tick.
- Cold multiplier Needs.ArrivalChill = 0.5 in WithSurroundings.
- Client: ArrivalBadge.cs in NeedsHud's gauge row after warmth; SurvivalClient.UpdateArrival drives it and posts the end note (not when a death ended it).

## Progress log
- 2026-10-16: built server and client per design; look proposed to UX/UI (options A badge [building], B glow on warmth ring, C sprout by the stamina bar). Not compiled locally (no dotnet).

## Open questions / blockers
- UX/UI to confirm the look (session_01V6qgbNbvTwniqW946KACLr).

## Next step
After #119 merges: merge main into claude/survival-arrival, open the survival-8 PR, wait for CI, send green sha to the PM, and a render request (badge on a fresh character) to the Render lane.
