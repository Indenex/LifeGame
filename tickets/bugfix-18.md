# bugfix-18: a dry pot beside a fire can't be picked up
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-18
PR: #108

## Goal
A dry pot sitting within 1.8 m of a burning fire (pots shaped at a campfire are set down 1 m from it) goes into the fire on G only when the fire can take it: fuel for the whole 4-minute firing and a free place in its 4-pot ring. Otherwise G picks it up, so it can be carried to a kiln. If the fire is low and you carry fuel, the prompt you face is "Feed the fire" (not a "Fire the pot" that only answers "Feed the fire first"); once fed, "Fire the pot" comes back.

Cause: server/spacetimedb/Ceramics.cs TendClay always tried to fire a pot beside a burning fire and threw "That fire is full of pots already" or "Feed the fire first", never falling through to the pick-up. The client (CeramicsView.TendOffer) offered "Fire the pot" at priority 22, above Feed the fire (20), so G repeated the error.

Fix: shared/ClayRules.FireCanTakePot(fuelSecondsLeft, potsInFire). The server's TendClay fires a dry pot only when it holds, else picks it up; the client's TendOffer counts a dry pot as beside the fire only when it holds. When it doesn't, "Pick up the pot" is offered with the firing offers (priority 22, above Feed the fire 20), except when the fire only lacks fuel and you carry some: then it is offered at 12 and Feed the fire wins. Wet pots behave as before.

## Progress log
- 2026-10-16: found in the clay-path review (see bugfix-17). Fixed; server and client build. Bot check (scratch, based on tools/ClayCheck: five pots by a fire, four fill the ring, G on the fifth) running against this module and main.
- 2026-10-16: bot check result: on main, G on the fifth pot fails with "That fire is full of pots already"; with this fix it lands in the bag ("ok   G on a dry pot by a full fire picks it up"). The low-fuel path (Feed the fire wins, then Fire the pot returns) is client logic, built but not bot-tested; it needs the Render lane's look.
- 2026-10-16: merged main (#99 conflict, then #109); new head 77d6990 then this merge.
- 2026-10-16: Render lane on e80c6cf: with a full ring and sticks in hand the prompt read "Feed the fire", and G fed a stick (pick-up at 12 lost to feeding at 20). Fixed: the pick-up now goes with the firing offers at 22 unless the fire only needs fuel and you carry some. Client builds.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, record the run link here, then send the PM the green sha.
