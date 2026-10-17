# crafting-28: Place a fire where you want
Lane: Crafting
Owner: Crafting thread
Status: in_progress
Branch: crafting-fireplace
PR: (open after the local checks; CraftCheck is blocked by the bot-walk problem until Bug fixes' bugfix-34 lands)

## Goal
Justin: "place fires where you want." Today `StartCraft("fire.*")` picks the spot itself (a step in front, nearest clear ground, a built or cold hearth within reach first). Server part (done): `StartFireAt(processId, score, x, z)` starts the same craft at a chosen spot. The spot is validated like the automatic one (finite numbers, within reach, not too close to stand, a raised or cold hearth beside it takes the fire, otherwise `ClearForFire`: level dry ground, no boulder/trunk, spacing from other fires and hearths). Only fire processes take a spot; nothing is used up on a refusal. Client part (UX owns the ghost/placement look and feel, D-154): a ghost ring for the fire like the item placing ghost of ui-16, then `StartFireAt`.

## Progress log
- 2026-10-17: server reducer and CraftCheck refusals (out of reach, non-fire) built; server, bindings, client, CraftCheck compile.

## Open questions / blockers
- Client ghost (UX): does the fire ghost snap to a hearth ring, and is the spot confirmed with a click, or is the fire lit straight away at the cursor? Options sent to the PM.
- CraftCheck cannot run until bugfix-34 lands.

## Follow-up (crafting-30, small PR after #547 and #582 merge; PM, 2026-10-17)
Integration's #569 census found 7 CraftCheck failures from fire spots on boulders or no level ground: not walking, CraftCheck choosing a bad spot. Make CraftCheck pick a validated fire spot with the same rules as the server (ClearForFire) before lighting. Plan: move the static part of ClearForFire (water level, slope over the 3x3 cells, boulder and trunk distances via Rocks/Trees, using a depth lookup) into shared/ as `FireGround.Clear(x, z, depthAt)`; the server's ClearForFire calls it and keeps the fire/hearth spacing and UnderStructure checks; CraftCheck scans rings around the bot with the shared check (plus its own fire list) and lights with `StartFireAt`; UX's ghost (#545) can show amber for bad ground with the same call. Tell UX when it lands.

## Next step
Send UX the API; when bugfix-34 is on main, merge main, run CraftCheck and tools/upgrade-check.sh origin/main, push, open the PR.
