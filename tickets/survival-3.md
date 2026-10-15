# survival-3: Carrying limit
Lane: Survival needs
Owner: Survival thread
Status: in_progress
Branch: claude/survival-needs-g7yrhi (restarted from main after #12 merged)
PR: (not yet)

## Goal
Justin chose a carrying limit (D-042). Every item has a load (roughly kilograms). You can carry a base amount, and a woven basket (Crafting item 13) raises it. When you're over the limit, something happens; Justin picks what. The HUD shows "Load X / Y" under the needs gauges, and it turns amber when you're over. Justin signs off on the numbers from a screenshot before they ship.

## Progress log
- 2026-10-15 22:40: shared/Carry.cs holds the weights, BaseLimit 20, BasketBonus 15, Basket = (Item)13 (swap for Item.WovenBasket when Crafting adds it), and WalkFactor, which drops from 1 at the limit to 0.5 at 1.5x the limit. The client shows the load under the gauges, notes "You are carrying too much", and Main multiplies the walk speed by survival.WalkFactor (a one-line hook). The server doesn't enforce the slowdown yet; Move still allows World.MaxSpeed.

## Open questions / blockers
- Waiting on Justin via the PM: base limit, basket bonus, and what happens when you're over (default: slower walk; the alternative is that you can't pick up more).
- Item 13 isn't in Items.cs yet. Until Crafting adds it, the client would throw if a basket stack showed up in the inventory.

## Next step
Send the PM the proposed numbers and the overloaded-HUD screenshot, then wait for Justin's call. After PR #12 merges, rebase these commits onto main and push.
