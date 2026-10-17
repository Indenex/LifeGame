# model-17: Spade on Item.Spade
Lane: Modeling
Owner: Modeling lane session
Status: done
Branch: claude/model-17-spade
PR: (see below)

## Goal
Crafting's spade (`Item.Spade`, id 18) is on main.
- ItemModels already had a painted spade model keyed by the raw number 18; it's now keyed by the enum.
- The Showcase pending stand-in is removed.
The spade's ground model, held model and inventory icon all draw in the smooth painted look.

## Progress log
- 2026-10-17: swapped (Item)18 for Item.Spade; build has 0 errors; the items view shows the spade painted.

## Open questions / blockers
- Audio's ToolSounds.cs still uses `const ushort SpadeItem = 18`; told Audio via the PM.

## Next step
Wait for CI on the PR, then send the green sha to the PM.
