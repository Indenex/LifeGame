# crafting-20: Reserve item ids for the bow and arrow
Lane: Crafting
Owner: Crafting thread (Sonnet, D-159)
Status: in_review
Branch: crafting-bow-ids
PR: (to open)

## Goal
Animals' bow and arrow (animals-15) needs item ids from the Crafting range (1 to 99). Modeling has the models ready (PR #235) and needs the numbers. This reserves them so Modeling and Animals can build on a shared id without waiting for any other crafting work: Item.Bow = 16 and Item.Arrow = 17, both with quality, no recipes, no tags yet. Nothing visible changes for players.

## Progress log
- 2026-10-16: checked every remote branch: ids 16 to 25 are free everywhere. Added the two ids and their ItemDefs in shared/Items.cs. Server module and client build locally.

## Open questions / blockers
- Animals owns the bow and arrow recipes and the hunting use (their Processes/ItemDef changes go in their branch). The bow will probably want to wear like other hand tools: when they add it, add Item.Bow to Durability.Wears in shared/Durability.cs (arrows are used up, so they don't wear).

## Next step
Open the PR, send the PM the green sha, and tell Modeling (session_01JKX3FNjkrEkAqoTaFJiXAm) and Animals (session_01SAcVBUUfPFFk1rdXuiQZRU) that Bow = 16 and Arrow = 17 on branch crafting-bow-ids.
