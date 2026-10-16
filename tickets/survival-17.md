# survival-17: Food as data (FoodDef)
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_review
Branch: claude/survival-fooddef
PR: (to open)

## Goal
docs/CONTENT.md rule (Justin, via Integration): content is data on shared bases. The Food row is Survival's. `shared/Foods.cs` adds `FoodDef` (item, food, water, good-meal flag, container left behind) and `PotDef` (full pot, empty pot, water), with `Foods.All` / `Foods.Pots`. `Needs.Nourishment` reads the registry; `Eat` and `DrinkFrom` use it; the old `EatenFrom`, `GoodMeal` and `PotOfWater` switches are gone. Behaviour unchanged: same numbers from the same Needs constants. CONTENT.md Food row updated.

## Progress log
- 2026-10-16: done; server and client build, tools/upgrade-check.sh origin/main passes (no schema change).
- 2026-10-16: CI server job failed on CraftCheck "building the CraftingTable: Out of reach" (run 38974318954): not this PR, fixed on main by bugfix-26 (#295). Merged main (also #319 for a client namespace break); builds clean.

## Open questions / blockers
- Roast path (Cooking.cs, Roasting) still hard-wired to mutton; next conversion step is a cook result on FoodDef.

## Next step
Open PR, wait for CI, send green sha to the PM (session_01CvoSQGRe3vDvDXWQu2qruU).
