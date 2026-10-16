# animals-6: Sheep keep clear of burning fires
Lane: Animals
Owner: Animals thread (session session_01XS7M1yLy2fLaFdE3HWnisM)
Status: in_review
Branch: claude/animals-sheep-5gkntn (restarted from main 47cc6e5 after #63 merged)
PR: #69

## Goal
Playtest found sheep wandering onto and standing in a lit fire. Sheep should keep about 1.5 m from a burning fire and shy from it like a mild fright. Burned-out fires don't count.

## How it works
- New `server/spacetimedb/AnimalFires.cs`:
  - `AnimalStepBlocked(ctx, from, to)`: a step is refused if it ends within `FireKeepAway` = 1.5 m of a burning fire (FuelUntil in the future) and brings the sheep closer. A step that moves it away is always allowed, so a sheep caught inside the ring can leave. Fires are looked up in the 3x3 chunks around the step through Fire.ByChunk.
  - `AnimalShyFromFires`: within `FireUneasyRange` = 4 m, nervousness rises by up to 0.45 × 0.7 per tick (closer is stronger) and the flee direction leans away from the fire. Next to a fire a sheep bolts a few meters; at 3-4 m it just drifts off.
- Animals.cs: `AnimalMove` takes ctx and treats a blocked step like the shore (a running sheep swerves, a walking one stops and picks a new spot). `AnimalShyFromFires` runs right after `AnimalSense`. Hunting.cs: the startle leap can't land in the ring either.
- `AnimalStepBlocked` is also the place House building's fence hook (building-6) goes.
- No schema change.

## Progress log
- 2026-10-16 04:35: written, builds. Local test: two fires, one lit at the flock's home spot; over 2 minutes the closest any sheep came to a fire was 1.64 m. Screenshot screenshots/animals-08-sheep-shy-of-fire.png.
- 2026-10-16 04:30: #63 merged; branch restarted from main 47cc6e5 with animals-6 merged in.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the animals-6 PR, then send the PM the green sha.
