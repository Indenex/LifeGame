# survival-18: Roast results as data (RoastDef)
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_review
Branch: claude/survival-roastdef
PR: (to open; stacked on #304 survival-17)

## Goal
Second step of the Food row (docs/CONTENT.md). `Foods.Roasts` (`RoastDef`: raw, cooked, burnt) supplies what comes off the fire; `Cooking.RoastResult` no longer names the mutton items. Behaviour unchanged. A new roastable is a RoastDef entry plus its recipe.

## Progress log
- 2026-10-16: done; server and client build; CraftCheck passes locally on a fresh world.

## Open questions / blockers
- Stacked on #304: merge #304 first (this branch contains its commits). The roast recipe in Processes.cs still names raw and cooked items itself; deriving it from Foods.Roasts is the next step.

## Next step
Open PR, wait for CI, send green sha to the PM (session_01CvoSQGRe3vDvDXWQu2qruU).
