# crafting-18: CraftCheck finds ground for its fires
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-check-fire-spot
PR: #203

## Goal
Main Build 653 (run https://github.com/Indenex/LifeGame/actions/runs/38953557618) failed intermittently in CraftCheck: "fire.hand_drill: There's no clear, level ground here for a fire". The second fire is started standing at the first, and the server's placement (#92: away from other fires, hearths, boulders and trunks) can find nothing within reach. CraftCheck's three fire crafts now go through `FireCraft`, which steps 4 m along (a new direction each time) and retries when the server finds no ground, and fails after 9 tries with the bot's position. The check is not skipped.

## Progress log
- 2026-10-16 20:05: done; CraftCheck passes twice locally, on a fresh world and again on the same world with the earlier fires still burning.

## Open questions / blockers
- (none)

## Next step
None. Merged to main; kept for history.
