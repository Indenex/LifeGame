# crafting-20: Reserve item ids for the bow and arrow
Lane: Crafting
Owner: Crafting thread (Sonnet, D-159)
Status: in_progress
Branch: crafting-bow-ids (ids, PR #266); crafting-bow (powers and recipes, stacked on it)
PR: #266

## Goal
Animals' bow and arrow (animals-15) needs item ids from the Crafting range (1 to 99). Modeling has the models ready (PR #235) and needs the numbers. This reserves them so Modeling and Animals can build on a shared id without waiting for any other crafting work: Item.Bow = 16 and Item.Arrow = 17, both with quality, no recipes, no tags yet. Nothing visible changes for players.

## Progress log
- 2026-10-16: checked every remote branch: ids 16 to 25 are free everywhere. Added the two ids and their ItemDefs in shared/Items.cs. Server module and client build locally.

- 2026-10-16 (branch crafting-bow, stacked on crafting-bow-ids): ItemDef.BowPower/ArrowPower (Bow 60, Arrow 0.65, the PM's option A until Justin picks), weights in Carry (bow 1.5, arrow 0.1), recipes lash.bow (3 sticks + 2 cordage, cutting tool) and carve.arrows (2 sticks + 2 fiber, 3 arrows, cutting tool). Server and client build. Still to do: BowShotHook in server/spacetimedb/CraftingBow.cs (needs Animals' claude/animals-bow-5gkntn for BowShot, and #267 for Durability.Wears(Item.Bow)); 'Things to try' lines to Journal for the two new process ids.

## Open questions / blockers
- Animals owns the bow and arrow recipes and the hunting use (their Processes/ItemDef changes go in their branch). The bow will probably want to wear like other hand tools: when they add it, add Item.Bow to Durability.Wears in shared/Durability.cs (arrows are used up, so they don't wear).

## Next step
When #266 and #267 are on main and Animals' bow branch has merged: merge main into crafting-bow, write BowShotHook in CraftingBow.cs (best working bow, one arrow, wear the bow), add Item.Bow to Durability.Wears, open the PR.
