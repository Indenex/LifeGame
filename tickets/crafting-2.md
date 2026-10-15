# crafting-2: Roast mutton, stone knife, digging stick, basket
Lane: Crafting
Owner: Crafting thread
Status: in_review
Branch: crafting-cook
PR: #20

## Goal
Per D-039: raw mutton (Animals drops it when butchering) roasts over a burning fire. "Roast mutton" in the crafting panel (Fire and stone tab) sets it on the nearest fire in reach, where it browns visibly. After 20 s it can come off (C, then 0) as roast mutton, best at about 30 s; after 60 s it is burnt. If the fire dies, the meat drops beside it as it was. Process id `cook.meat`; items RawMutton 190, CookedMutton 191, BurntMutton 192 (agreed with Animals, who start at 193).

Also a stone knife (haft.knife), a digging stick (carve.digging_stick) and a woven basket (weave.basket). Justin's calls: D-041 the digging stick digs one level deeper than hands, to finer clay (quality 80, matching Ceramics' ClayRules.DugClayQuality); D-042 the basket raises a carry limit that Survival adds, so here it is only a craftable item with the Container tag; D-043 cooking trains Firecraft (Skills' mapping).

## Progress log
- 2026-10-15: shared items and `Roasting` timings, server `Roast` table with TakeOffFire (server/spacetimedb/Cooking.cs), roasts drawn on the campfire, take-off rows in the panel. CraftCheck has an optional roast step (CRAFTCHECK_ROAST=1, mutton seeded by hand) and it passes locally: early take-off refused, roast mutton quality 80 at 30 s.

- 2026-10-15: knife, digging stick and basket added; PR #20 opened. Reworked to D-041/D-042: no gather-all basket, the digging stick reaches one more level. CraftCheck covers all three tools and the deep dig.

## Open questions / blockers
- Eating roast mutton needs a Nourishment entry in Survival.cs (Survival lane, asked).

## Next step
Send the PM the meat-on-fire screenshot and the green head sha of PR #20; switch Dig to ClayRules.DugClayQuality once Ceramics lands it on main.
