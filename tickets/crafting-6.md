# crafting-6: Fire fuel readout
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-fire-fuel
PR: (see STATUS)

## Goal
From the crafting-5 playtest: a new player can't tell how long their fire will last, and pit firing needs about 4 minutes of fuel. The G prompt on a fire now says how long it has left: "Feed the fire (about 3 min left)", "(1 min left)", or "(dying)" under 30 s. Justin sees the time left whenever he stands at a fire carrying fuel.

## Progress log
- 2026-10-16 00:40: FireView.TimeLeft(fire) plus the feed prompt in Main.cs (one-line hook). Client builds.

## Open questions / blockers
- (none)

## Next step
None. Merged to main; kept for history.
