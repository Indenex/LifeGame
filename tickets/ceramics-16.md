# ceramics-16: "Coil pot" is now "Clay pot" (display names only)
Lane: Ceramics
Owner: Ceramics thread
Status: done
Branch: claude/clay-ceramics-16-kfr4kr
PR: #522

## Goal
Justin asked "what is a coil pot.." and picked the rename "Coil pot" to "Clay pot". Only display names change: item names and plurals (Clay pot, Clay pot of water, Unfired clay pot), the process name "Build a clay pot" and its "Building a clay pot" line, and the pot's form name in messages. Enum names (`Item.CoilPot`, `ClayForm.CoilPot`), numbers and process ids (`clay.coil_pot`) are unchanged. The coiling minigame keeps its coil wording because that is what the player does.

## Progress log
- 2026-10-17 04:30: renamed in shared/Items.cs, shared/Processes.cs, shared/ClayRules.cs. Journal hint (`clay.coil_pot` in NextSteps.cs) does not name the pot, so no change needed there; wiki text is Wiki lane's.

- 2026-10-17: #522 merged. Nothing left.

## Open questions / blockers
- Tell the Journal and Wiki lanes about the new names (PM asked the lane to do this).

## Next step
Merged. Nothing left.