# crafting-16: Craft times by tier, work and quality (D-113)
Lane: Crafting
Owner: Crafting thread
Status: in_progress
Branch: crafting-craft-times
PR: (not yet)

## Goal
D-092/D-113/D-126: craft times scale. Cheap, simple items are fast; higher tiers and better results take longer; skill shortens them (Journal's linear DurationScale, unchanged), and so does a better tool (D-089).

## Design
time = Base × QualityTimeScale × DurationScale(rank) × (1 − ToolSpeedup)
- `Processes.Base(tier, effort)`: tier seconds (3, 6, 12, 20 for tiers 0 to 3) × effort. Fires (4 s, 3 s) and roasting (1 s) keep their own times.
- `Processes.QualityTimeScale(q)` = 0.8 + 0.4 × q/100, from the quality the job will come out at. Not applied to fires and roasts.
- New base times (old in brackets): tinder 3 (3), flake 3 (3), hand axe 9 (8), digging stick 6 (5), cordage 6 (6), spear 12 (8), stone axe 18 (10), knife 12 (8), basket 30 (15), sleeping mat 24 (12), bow drill 12 (8), chop 15 (15), scrape hide 12 (8), stretch hide 6 (6), clean/grit/grog/temper/daub 5 (3-5), pinch pot 15 (6), coil pot 40 (12), storage jar 60 (16), brick 10 (5).
- Check tools wait def.Seconds × 1.2 + 5 (CraftCheck, ClayCheck, BuildTest).

## Progress log
- 2026-10-16 19:40: implemented on main. Server and client build; CraftCheck passes (4 min 16 s).

## Open questions / blockers
- The sleeping mat (House building), hides (Animals) and clay (Ceramics) times change too, under the same formula. Lanes told.

## Next step
Finish the ClayCheck run, open the PR, and send the PM the green sha.
