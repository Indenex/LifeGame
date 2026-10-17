# crafting-25: Maker's mark (who made it)
Lane: Crafting
Owner: Crafting thread
Status: todo (starts when UX's #443 is on main)
Branch: crafting-maker
PR: (none yet)

## Goal
Justin: "if an item is inspected, it tells you who made it." Made things remember their maker. Server column `[Default(0u)] uint Maker` (a PlayerId, 0 = unknown/none) appended after #443's Slot and OnBelt on InventoryItem, and on WorldItem, BasketItem and ClaimStash. Different makers do not stack (PM and Justin confirmed). UX shows "Made by" in the client from the rows; the client resolves the name from the player table (no name column).

## Conditions from Integration (via UX), all required
1. One shared "same stack" helper (item, quality, wear, maker) used by every merge site: FindStack, AddToBag, ToBag in Inventory.cs (#443), trade (MoveCarried), basket put and take, stash, pick-up, drop, death drop, and WearTool's split. No sprinkled `&& Maker ==`. List the sites in the PR.
2. Only the server writes Maker, when a craft finishes (FinishCraft; Ceramics' kiln and drying output pass the player id via an optional AddToInventory argument); never from a client argument. Copied unchanged through every move. A mended tool keeps its maker.
3. Append after Slot and OnBelt; merge main before the final CI. Upgrade-safe: additive, default 0, `tools/upgrade-check.sh origin/main` passes.
4. No name column.

## Progress log
- 2026-10-17: PM and Justin confirmed options; Integration conditions recorded here.

## Open questions / blockers
- Waits for #443 (UX belt/bag order) to merge.
- Ceramics (kiln/drying outputs) must pass the maker: tell them when the optional argument exists.

## Next step
When #443 is on main: merge main into crafting-maker, add the column and the helper, use it at every site above, wire FinishCraft, run CraftCheck and the upgrade check, open the PR.
