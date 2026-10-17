# building-20: basket rows show who made an item
Lane: House building
Owner: House building thread (Sonnet)
Status: done
Branch: claude/house-building-8ueqz2-maker
PR: #685 (merged)

## Goal
After UX's "Made by" (#677): the basket window's two grids (what's in the basket, and what you carry) build `BagStack`s with the row's maker, so hovering an item there says who made it, as in the bag.

## Progress log
- 2026-10-17: both `new BagStack(...)` in BuildingSystem.Use.cs pass the sixth argument, the row's own `Maker`, else a clay pot's mark in `Wear` (`Marks.MakerOf`), the same rule as the bag. Client builds.

## Next step
Nothing; merged.
