# survival-19: Roast recipes from RoastDef
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: done
Branch: claude/survival-roast-recipes
PR: merged, first commit fa341435

## Goal
Last step of the Food row (docs/CONTENT.md). The cook.meat and cook.venison recipes in Processes.List are built by `Processes.Roast(Item raw)` from the item and wording on `Foods.Roasts` (`RoastDef` gains Name and Doing), so the raw/cooked items and text live in one place. The two entries stay where they were in the list so the Journal order does not shift. Behaviour unchanged.

## Progress log
- 2026-10-17: done; server and client build. CraftCheck running locally on a fresh world.

## Open questions / blockers
- (none)

## Next step
Done and merged; nothing further on this ticket.
