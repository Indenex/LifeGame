# bugfix-4: Inventory HUD crash on equal labels ("Key: Clay")
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: done
Branch: claude/bugfix-lyle6o (ticket only; no code change needed)
PR: none (fixed on main by 1b1ca8b)

## Goal
Main.RefreshInventory threw "An item with the same key has already been added. Key: Clay" when two clay stacks of different quality were carried. Fix it, check the UX lane's new inventory bar (#26) for the same pattern, and tell UX.

## Progress log
- 2026-10-15 23:10: already fixed on main. 1b1ca8b ("crafting-1: group inventory lines by item and grade", 21:31) groups stacks by (item, grade) before building the dictionary. Clay has no quality, so all clay becomes one line whatever its quality. Every branch on origin has that version of RefreshInventory except ux-ui (which builds a list instead) and the old phase0/valheim spikes, so the crash Skills saw came from a build older than 1b1ca8b. Checked with a scratch program over every item in shared/Items.cs, 5 qualities and counts 1/2/7: 0 label clashes between different (item, grade) groups, so the dictionary key is unique.
- UX branch: `InventoryBar.SetItems` also does `items.ToDictionary(i => i.Label, ...)`. It is safe while RefreshInventory groups by item and grade, but it would throw again if anyone passes ungrouped stacks. Suggested to UX via the PM: key `last` on `(Item, Label)` or build it with GroupBy so it can't throw.

## Open questions / blockers
- (none)

## Next step
None.
