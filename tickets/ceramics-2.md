# ceramics-2: Updraft kiln
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session cse_017sscCMDfHcyeZNqGW22GBE)
Status: in_progress
Branch: claude/clay-ceramics-kfr4kr (restarted from main 8b96a02 after #11 merged)
PR: (none yet)

## Goal
A player builds an updraft kiln from sun-dried bricks and daub, loads dry pots into it, fuels and lights it, and gets back pots that rarely crack and come out a little better than pit-fired ones. Done when the kiln works end to end on a local server (ClayCheck extended), with screenshots of the kiln cold, firing, and unloaded.

## Design (defaults sent to the PM 2026-10-15; change if Justin decides otherwise)
- Build: process `clay.kiln`, 8 Brick + 2 Daub, 20 s, Outcome.Placed: a kiln appears just in front of you. At most 2 kilns per player.
- Load: V beside a cold kiln moves up to 6 unfired pots from your bag into it.
- Fire: V beside a loaded kiln burns fuel from your bag (sticks or any fuel item, 180 s worth, so 2 sticks) and fires for 3 minutes (D-013).
- Result: each pot cracks 2% of the time (vs 4-24% in a pit) and comes out 10 quality points better, capped at 100. Pots drop beside the kiln; a news line tells whoever lit it.
- Server: own `Kiln` table + `KilnTimer` in `server/spacetimedb/Kiln.cs`; the load is stored on the kiln row, so `ClayPiece` doesn't change and no world wipe is needed.
- Client: brick beehive kiln with a chimney under `client/scripts/ceramics/`; it glows and smokes while firing, with a label showing load and time left.

## Progress log
- 2026-10-15 22:10: ticket created; branch restarted from main.

## Open questions / blockers
- Sent to PM: the design defaults above.
- After Claims PR #8 is on main: RequireClaimAccess in TendClay, FirePots, OnPlaced and kiln use.

## Next step
Write server/spacetimedb/Kiln.cs (table, OnPlaced branch for clay.kiln, UseKiln reducer, finish timer), then the client view.
