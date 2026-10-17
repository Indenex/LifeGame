# animals-18: animals hear a player splashing through water
Lane: Animals
Owner: Animals lane (Sonnet)
Status: in_review
Branch: claude/animals-wading-5gkntn
PR: (see below)

## Goal
A player moving through water (wading or afloat, Wading.cs) splashes, and animals notice it: a moving player in water spooks animals out to their notice range even when sneaking, a little less than a charge (fear 0.5 close in, fading to nothing at the edge of the notice range). Standing still in the water is silent. Gentle by design (D-075): it only matters when you wade near animals, and it makes crossing a ford near a flock or a deer herd feel like crossing a ford.

## How it works
- `Animals.cs`: `AnimalWatcher.InWater` (bed lower than the water level by more than 5 cm, from `Wading.Depth`), set in `AnimalWatchPlayers`; `AnimalSense` raises a watcher's threat to `WadeSplashFear * near` when they are in water and moving.
- No schema change.

## Progress log
- 2026-10-17 02:10: written; server and client build, upgrade check against main passed. Not played (no bot test of water yet).

## Open questions / blockers
- Design call for Justin (a player would notice it): the default is the splash spooks animals even when sneaking. Options: (a, current) splash spooks within the notice range; (b) only spooks within half the notice range, so a careful wader can still approach; (c) no effect, as before. Sent to the PM.

## Next step
Wait for CI; send the PM the green sha; try it with a bot at a ford if the PM wants numbers.
