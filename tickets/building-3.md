# building-3: Kiln support, hut door, storage basket
Lane: House building
Owner: House building thread
Status: in_progress
Branch: claude/house-building-8ueqz2 (pushed after #10 merges; local branch building-3 until then)
PR: (new PR after #10 merges)

## Goal
Ceramics can place an updraft kiln from the B menu as a blueprint (D-035): Building provides the Solid round-shape flag and the CeramicsStructureRemoved hook (done in #10). A finished clay hut can get a woven hurdle door that G opens and closes; closed, it keeps more warmth. A coiled-fiber storage basket can be placed from the B menu and holds items that G opens.

## Progress log
- 2026-10-15 22:40: kiln hooks landed in #10 (e43c0f3). Design questions on the door and basket sent to the PM.

## Open questions / blockers
- Sent to the PM for Justin: who can open a door (default anyone); who can take from a basket (default anyone, claim members only inside a claim); does a basket weather (default no).

## Next step
Add a StructureDoor table and ToggleDoor reducer for complete clay huts (default: anyone may open), door piece in StructureModel, warmth 0.9 when closed.
