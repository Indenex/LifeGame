# bugfix-22: "Fill pots with water" offered on banks where the server refuses it
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-22
PR: (opening)

## Goal
The G prompt "Fill pots with water" shows only where pressing it works. It no longer answers "Stand at the water's edge to fill a pot" on a sloped bank.

Cause: the client (client/scripts/ceramics/CeramicsView.cs FillOffer) sampled interpolated ground height every 0.5 m within reach. The server (server/spacetimedb/Ceramics.cs NearWater) needs a grid point under water within reach. Between grid points the interpolated surface dips under water on a bank where no grid point does. Fix: the client checks the same grid points with reach trimmed by 0.2 m, as the Drink check does. That is a strict subset of what the server accepts, so an offer the server would refuse can't appear.

Found in the PM-requested env-2 playtest review: on env-2's land, 1,608 of 20,337 sampled offers (7.9%) would be refused. The bug exists on main too, but water there is rarer.

## Progress log
- 2026-10-16: fixed; the client builds.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, record the run link here, then send the PM the green sha.
