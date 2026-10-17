# crafting-38: The hand empties when its item leaves the bag
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-hand
PR: (see below)

## Goal
The cosmetic leftover noted in crafting-37: the item in hand (`PlayerEquip.Item`, what other players see) stayed set after the carried item ran out (used up, dropped, hung on the belt, lost in a death) until the next hotbar change. `ClearHandIfGone` (`Equip.cs`) now runs after `RemoveFromStack` (the central removal), `PutOnBelt` and the death drop; it clears the hand when the item was in hand and none is left in the bag, and stops a chop that needed it (`CheckJobTool`). Mending an equipped tool removes and re-adds it, so `RepairTool` restores the hand afterwards. CraftCheck: hanging the axe on the belt empties the hand.
Also: a server log line for each capped (`score capped: ...`) or softened (`score softened: ...`) minigame score (`CraftScore.cs`), so a play-test (Render C) can tell whether honest play ever meets the 0.6 quick cap: read with `spacetimedb-cli logs <db>`.

## Next step
Wait for CI, send the green sha to the PM; then Render C reports the play-test.
