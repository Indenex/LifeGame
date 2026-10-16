# survival-3: Carrying limit
Lane: Survival needs
Owner: Survival thread
Status: in_review
Branch: claude/survival-needs-g7yrhi (restarted from main after #12 merged)
PR: #39

## Goal
Justin chose a carrying limit (D-042). Every item has a load (roughly kilograms). You can carry a base amount, and a woven basket (Crafting item 13) raises it. When you're over the limit, something happens; Justin picks what. The HUD shows "Load X / Y" under the needs gauges, and it turns amber when you're over. Justin signs off on the numbers from a screenshot before they ship.

## Progress log
- 2026-10-15 22:40: shared/Carry.cs holds the weights, BaseLimit 20, BasketBonus 15, Basket = (Item)13 (swap for Item.WovenBasket when Crafting adds it), and WalkFactor, which drops from 1 at the limit to 0.5 at 1.5x the limit. The client shows the load under the gauges, notes "You are carrying too much", and Main multiplies the walk speed by survival.WalkFactor (a one-line hook). The server doesn't enforce the slowdown yet; Move still allows World.MaxSpeed.
- 2026-10-15 22:50: merged main (Use-key registry #17). Forage, drink at water, and drink from a pot are Use handlers on G (the pot is priority 1, offered only when thirsty and away from water), and R is unbound. Screenshot of an overloaded HUD (29.9 / 20 in amber, with a G prompt): project files screenshots/11-survival-carry-overloaded.png. Numbers sent to the PM for Justin.
- 2026-10-15 23:45: PM said build on slower walk. Merged main (quieter HUD #26). The load text is replaced by a fourth ring with a sack glyph, shown from 80% of the limit and amber and breathing when over; UX agreed. The server's Move scales its speed check by Carry.WalkFactor. Added KillPlayer(ctx, player, cause) and NeedsNow(ctx, playerId) for Claims PvP. The bush reach is now shared as Needs.BushReach 2.5, and the client offers picking at 2.3. A ripe bush within 60 degrees of facing is the survival.forage_facing Use handler at priority 25, so it beats feed fire (20). Screenshot: project files screenshots/12-survival-carry-ring.png.
- 2026-10-15 23:55: dropped the old survival prompt box (UX asked); Use and hints carry the prompts. Follow-ups once ui-4 (#38) is on main: fade the needs rings while Panels.Open(); forage passes Target and forage_facing goes away; Hints.Done("eat").
- 2026-10-16 00:16: CI green on 02016f7 (run 38827525198), which contains main cb9d155. The basket is Item.Basket. Sha sent to the PM.

## Open questions / blockers
- Justin's card on the numbers is open; slower walk is the shipped default.

## Next step
The PM merges #39. Then do the ui-4 follow-ups (Panels fade, Use Target, Hints.Done) and D-039 mutton eating once those items exist.
