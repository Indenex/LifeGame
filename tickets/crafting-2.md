# crafting-2: Roast mutton, stone knife, digging stick, basket
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-cook
PR: #20

## Goal
Per D-039: raw mutton (Animals drops it when butchering) roasts over a burning fire. "Roast mutton" in the crafting panel (Fire and stone tab) sets it on the nearest fire in reach, where it browns visibly. After 20 s it can come off (G by the fire, or C then 0) as roast mutton, best at about 30 s; after 60 s it is burnt. If the fire dies, the meat drops beside it as it was. Process id `cook.meat`; items RawMutton 190, CookedMutton 191, BurntMutton 192 (agreed with Animals, who start at 193).

Also a stone knife (haft.knife), a digging stick (carve.digging_stick) and a woven basket (weave.basket). Justin's calls: D-041 the digging stick digs one level deeper than hands, to finer clay (quality 80, matching Ceramics' ClayRules.DugClayQuality); D-042 the basket raises a carry limit that Survival adds, so here it is only a craftable item with the Container tag; D-043 cooking trains Firecraft (Skills' mapping).

## Progress log
- 2026-10-15: shared items and `Roasting` timings, server `Roast` table with TakeOffFire (server/spacetimedb/Cooking.cs), roasts drawn on the campfire, take-off rows in the panel. CraftCheck has an optional roast step (CRAFTCHECK_ROAST=1, mutton seeded by hand) and it passes locally: early take-off refused, roast mutton quality 80 at 30 s.

- 2026-10-15: knife, digging stick and basket added; PR #20 opened. Reworked to D-041/D-042: no gather-all basket, the digging stick reaches one more level. CraftCheck covers all three tools and the deep dig.
- 2026-10-15: merged main (Skills, Animals, Use key); G takes meat off the fire; tools are not fuel. CI green at e600b3e, sent to the PM to merge.
- 2026-10-15: merged main again for #18 (roast query in net/Subscriptions.cs); CI green at 310d8ba (run 38820022479); PR #20 merged by the PM.

## Open questions / blockers
- Eating roast mutton needs a Nourishment entry in Survival.cs (Survival lane, asked).

## Next step
Done: PR #20 merged 2026-10-15. Ceramics folds ClayRules.DugClayQuality into GiveDugClay in #21.
