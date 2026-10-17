# crafting-21: Craft output and placing items
Lane: Crafting (UX does the drag, ghost and prompt)
Owner: Crafting thread (Sonnet, D-159)
Status: done
Branch: crafting-place (stacked on crafting-durability, PR #267)
PR: (to open)

## Goal
Justin (via the PM): finished crafts go to the bag; real-world items (bricks, pots, jars, logs, baskets) can then be set down where you choose. UX drags the item out of the bag onto the ground with a ghost that follows the cursor; this ticket is the server and shared side. Every craft already ends in the bag, and carrying never blocks (it only slows you), so there is no bag-full case.

## What it adds
- shared/Items.cs: ItemDef.Placeable (Brick, fired PinchPot and CoilPot, StorageJar, Basket, Log).
- shared/Placement.cs: Placement.Check(item, x, z, groundAt) returns a reason (water, steep ground, not placeable) or null; the client ghost and the server share it.
- server/spacetimedb/Placing.cs: PlaceItem(item, quality, wear, x, z, yaw): reach, claim access, the shared check, then trees and boulders ("Something is in the way"); takes one from the bag and lays a WorldItem with Placed = true and Yaw.
- WorldItem gets defaulted Placed and Yaw columns (no wipe). Client Main.cs uses Yaw when Placed.
- CraftingUi.Finished(ProcessDef, Item, byte): raised when an Outcome.Item craft finishes (not chopping or placed crafts).
- CraftCheck covers a refused stick, an out-of-reach refusal, a placement keeping its yaw, and picking it back up.

## Progress log
- 2026-10-16: built on crafting-durability; server, client and CraftCheck pass locally; upgrade check run.

- 2026-10-16: PM's ask bundled here: CraftJob.Tool and CraftWork.Tool (ushort item id, [Default] 0, no wipe) set in StartCraft from the tool BestTool picked, so other players see the exact tool in hand (Animation anim-3, #372). CraftCheck checks a tool craft's work row names a tool. Merged the latest crafting-durability head.

## Open questions / blockers
- UX wires ServerPlaceLink once this is on main (they are building against a mock on claude/ux-ui-place-xlrh2s).
- Fired pots and bricks come out of the kiln or the drying stage, not an Outcome.Item craft, so Finished won't fire for them; Ceramics can raise a prompt from their own finish if Justin wants it.

## Next step
None. Merged to main; kept for history.
