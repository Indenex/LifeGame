# crafting-3: Roast mutton over a fire
Lane: Crafting
Owner: Crafting thread
Status: in_progress
Branch: crafting-cook
PR: (not yet)

## Goal
Per D-039: raw mutton (Animals drops it when butchering) roasts over a burning fire. "Roast mutton" in the crafting panel (Fire and stone tab) sets it on the nearest fire in reach, where it browns visibly. After 20 s it can come off (C, then 0) as roast mutton, best at about 30 s; after 60 s it is burnt. If the fire dies, the meat drops beside it as it was. Process id `cook.meat`; items RawMutton 190, CookedMutton 191, BurntMutton 192 (agreed with Animals, who start at 193).

## Progress log
- 2026-10-15: shared items and `Roasting` timings, server `Roast` table with TakeOffFire (server/spacetimedb/Cooking.cs), roasts drawn on the campfire, take-off rows in the panel. CraftCheck has an optional roast step (CRAFTCHECK_ROAST=1, mutton seeded by hand) and it passes locally: early take-off refused, roast mutton quality 80 at 30 s.

## Open questions / blockers
- Eating roast mutton needs a Nourishment entry in Survival.cs (Survival lane, asked).

## Next step
Screenshot of meat on the fire, then open the PR and send it to the PM.
