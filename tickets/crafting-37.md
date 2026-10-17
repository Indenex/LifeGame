# crafting-37: Only a carried item comes to hand
Lane: Crafting
Owner: Crafting thread
Status: in_review
Branch: crafting-equipfix
PR: (see below)

## Goal
Follow-up to the crafting-35 audit, from the hotbar/equip check. A hotbar slot keeps its item while the player has none ("it fills again when you pick more up"), and `EquipSlot` put whatever the slot named into `PlayerEquip` whether it was carried or not. Gameplay was safe (every tool check also needs the item in the bag), but `PlayerEquip.Item` is what other players see in hand, so a modified client could show itself holding any known item, and a normal player could show an item they had dropped. `EquipSlot` and `SyncEquip` (`server/spacetimedb/Equip.cs`) now bring an item to hand only when it is carried in the bag (not on the belt, which is storage). CraftCheck: a slot holding an uncarried item equips nothing.

## Checked, no change needed
`SetHotbarSlot` / `MoveHotbarSlot` (slot range, known item), `UseItem` (known item, must be in hand, must be carried), `MoveInBag`, `SortBag`, `PutOnBelt`, `TakeOffBelt` (rows must be the caller's own, place ranges checked, only tools on the belt).

## Not fixed
The hand is not cleared when the carried item later runs out (dropped, used up) until the next hotbar change: a stale cosmetic only. A hook on inventory changes would fix it; not worth the per-change cost now.

## Next step
Wait for CI, send the green sha to the PM; tell UX (hotbar is theirs; nothing needed from them).
