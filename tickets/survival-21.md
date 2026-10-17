# survival-21: Building-session check and the stone-ringed hearth's warmth
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_review
Branch: claude/survival-hearth-warmth
PR: (to open)

## Goal
Justin's focus is a small town. A 2 h building session needs hunger and thirst that nudge at a comfortable pace and warmth that does not turn into tending. `tools/session_sim.py` models it from the real constants (kept in the repo; rerun when the rules change). Findings (sent to the PM): the low nudge arrives after about 45 min (water) and 67 min (food) from full; one waterskin covers 24 min, a coil pot 60 min; warmth is the real limiter (a bar lasts 16 min away from a fire by day, 8 min on a winter night; a campfire gains warmth only within about 4.4 to 4.7 m). Justin picked a bigger stone-ringed hearth. This ticket gives House building the warmth side: `Fires.StoneRingWarmthRadius = 9` (first 8; the PM's default of 9 gives an 8 m work ring with about one warm-up an hour, break-even 7.9 m on a winter night) (linear falloff, as for a campfire), `Fires.WarmthFrom(..., radius)`, and `FireWarmthRadiusHook(ctx, fireX, fireZ, ref radius)` called by `WarmthAt` for each burning fire. With 8 m a winter-night builder gains warmth out to 7.0 m (campfire 4.4 m) and needs 0 warm-ups an hour inside that; at 8 m the fire's warmth is nil, so the ring of no-trips is about 7 m. For a full 8 m work ring, the radius would need to be about 9.1 m.

## Progress log
- 2026-10-17: sim written and committed; radius constant, WarmthFrom parameter and hook added; server and client build. No behaviour changes until House building implements the hook for its hearth (nothing implements it yet, so every fire still warms to 5 m).

## Open questions / blockers
- House building: build the stone-ringed hearth structure (stone ring, no walls) and implement `FireWarmthRadiusHook` (set `radius = Fires.StoneRingWarmthRadius` when a finished stone-ringed hearth is under the fire, as `FireFuelScaleHook` finds hearths). Client fire light/visuals can stay as is.

## Next step
Open PR, wait for CI, send the green sha to the PM (session_01CvoSQGRe3vDvDXWQu2qruU) and tell House building (session_01RrKHUxWRH973Ndnxsad9nv) the hook is on main.
