# crafting-29: World tool actions need the tool equipped
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-equip
PR: (none yet)

## Goal
Justin (via the PM, high priority): "I've noticed when loot is near trees, it's very hard to loot. Make it so for a tool to be used, it needs to be on your bar and equipped."
Rules (from the PM, 2026-10-17):
1. World tool actions need the tool equipped: chop (axe), dig (spade or stick), mine, cut grass and so on only happen when the matching tool is in the selected, equipped hotbar slot. A tool in the bag or on the belt does not count for these. Enforce on the server (D-182): the reducers check the player's equipped slot, not just inventory.
2. No tool equipped (or a non-tool) means interaction goes to looting: E near a tree picks up nearby loot (items, sticks, stones, berries) and never offers the tree action, and the prompt shows the loot. With an axe equipped near a tree, the prompt offers chopping, but loot that is clearly closer or under the cursor still wins; a second prompt line can show the other option.
3. Gentle hint: looking at a tree with nothing equipped and no loot nearby shows a soft hint, "Equip an axe to chop", once in a while, not every time.
4. Crafting recipes that need a tool (bench, cutting edge and so on) keep D-263: belt and bag tools still count there. Say if there is a conflict.
5. Hold-to-repeat (#495) keeps working with the equipped tool.
Coordinate the interaction priority part with Integration (session_015g3pLB5eJ4qPrXAwJUmkYT), who owns the Use dispatcher. Update CraftCheck and BuildTest bots to equip tools before using them. Send the PM the PR and green sha, and ask Render for a clip (loot next to a tree, with and without an axe).

## Progress log
- 2026-10-17: ticket written. No "equipped slot" existed on server or client. Built on branch crafting-equip (head 5433ad29):
  - server/spacetimedb/Equip.cs: public table PlayerEquip {Owner, Slot} (no row = nothing equipped), reducer EquipSlot(index) (pressing the equipped slot or 255 puts it away), EquippedTool(ctx, tags) (item on the equipped slot with the tags, carried in the bag not the belt, not spent) and EquippedToolOrThrow with plain messages.
  - Enforced in Dig (the digging tool; bare hands still dig), chop.log (StartCraftCore: Outcome.Chop uses the equipped tool, also for tool speed and wear), ShearSheep and ButcherCarcass. Crafting recipes keep BestTool (belt and bag, D-263). Melee strikes and the bow are unchanged (asked the PM).
  - Client: client/scripts/crafting/Equipped.cs (Item, Has), Woodcutting offers a chop only when Equipped.Has(Chopping), subscription player_equip WHERE owner = :sender.
  - Bots: CraftCheck (EquipTool helper, chop and both dig loops, new assertion that a chop with the axe unequipped is refused), ClayCheck (EquipAxe for chop and butcher), BuildTest (bot equips the axe on first chop).
  - Messages sent: PM (design, the ui-18 belt conflict, scope), UX (slot-select key, highlight, API), Integration (E priority, second prompt line, soft hint).
- 2026-10-17 (later): Justin answered via the PM: belt = storage only (belt tools count for crafting recipes, not for world use); spear and bow are included. Combat done in this PR: StrikeAnimal takes the equipped spear or blade (Equip.cs NoWeaponMessage), BowShotHook takes the equipped bow (arrows from the bag). ClayCheck equips before strikes. Upgrade check passes locally (additive table). Release is out; CI is free.
- Not run locally: CraftCheck/ClayCheck/BuildTest (the bot-walk problem); CI is the check.

- 2026-10-17 (later): Justin chose "every slot equips": EquipSlot now equips any non-empty slot (tool or not); an empty slot, the equipped slot again, or 255 puts it away. PlayerEquip gained `Item` (default 0), kept in step by EquipSlot and SyncEquip (called after SetHotbarSlot and MoveHotbarSlot), so other clients can show the tool in hand (Animation). New reducer `UseHeld()` uses the held item's Def (eat, drink, wear) through UseItem, the item taken from PlayerEquip, never a client argument. Subscription is now all of `player_equip`. Client `Equipped.Item` reads PlayerEquip.Item. Open with UX: whether UseItem(item)/PlaceItem should require the item to be the held one (Justin: "use is a click on the held item"); not enforced yet.

## Open questions / blockers
- PM: does a tool on the tools belt count when it is also on the bar? (recommended: no.) Do melee strikes and the bow need equipping? (recommended: not in this PR.)
- UX: slot-select key and highlight (their file). Integration: loot-versus-tree priority, second prompt line, soft hint "Equip an axe to chop".
- When #500 (spade) merges: Dig's BestDigger/DigSpeed must use EquippedTool, and the CraftCheck spade section must EquipTool(Item.Spade).
- CI hold from the PM (release).

## Next step
None: merged (#582, #615). Later sections below are history.

## Update 2026-10-17: equip-axe hint
- `Woodcutting.cs` registers `Use.RegisterHint("crafting.equip_axe", ...)` (Integration's #579): at a standing tree, carrying a working axe that is not in hand, with no loot within reach, the hint reads "Take your axe in hand to chop: press its number key". The chop handler keeps Priority 10, equal to the loot handlers.
