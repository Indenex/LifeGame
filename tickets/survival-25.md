# survival-25: Roast recipes come from the food data
Lane: Survival
Owner: Survival thread
Status: in_review
Branch: claude/survival-roast-data
PR: (see STATUS)

## Goal
A new roast food is one `RoastDef` entry in `shared/Foods.cs` and nothing else (D-206): `Processes.List` builds every roast recipe from `Foods.Roasts` instead of one `Roast(Item.X)` line each. Nothing changes for players except that the roast recipes now sit together in the recipe lists, ahead of the pot dishes (broth, stew, tallow).

## Progress log
- 2026-10-17 07:55: done. The same 47 process ids, names, tiers, inputs and results before and after (compared with a throwaway program); server module and client build.

## Open questions / blockers
- None.

## Next step
Wait for CI, then send the PM the green sha.
