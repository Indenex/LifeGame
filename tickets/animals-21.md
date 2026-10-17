# animals-21: Deer daily rhythm (midday bedding)
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-deer-rhythm-5gkntn
PR: (to open)

## Goal
Part of the rest of deer ecology. Deer (`AnimalDef.BedsAtMidday`) lie down where they stand through the middle of the day (time of day 0.42 to 0.62; a game day is 90 real minutes, so about 18 minutes) and get up again afterwards, staggered up to 20 s, so a hunter learns that dawn and dusk are the times to find them grazing. It reuses `AnimalState.Roost` (the client crouches a roosting animal) and the shared fawn-copies-mother code. Only a calm animal that isn't walking settles; any alarm makes it bolt as before and it beds again when calm. Sheep and hens are unchanged (hens still roost at night).

## Progress log
- 2026-10-17: built (`MiddayBed` in `Roost.cs`); server and client build; upgrade check passes. Not live-tested: deer herds are far from the waking clearing, so the load bots never wake them. PM asked for options on the rest of the ecology (rut, fawn look); default 2 (this) then 1.

## Open questions / blockers
- Justin: rut (stags roar and strut in autumn, bolder), fawn spots (Modeling), per the options sent to the PM.

## Next step
Wait for CI, send the green sha to the PM.
