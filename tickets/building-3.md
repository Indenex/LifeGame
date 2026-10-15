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

- 2026-10-15 23:00: Justin's answers (D-046): anyone opens doors; baskets open to anyone except in a claim (members only); player-made things outside claims weather after 72 h untouched, gone 24 h later. Built: HangDoor/ToggleDoor (closed door: warmth 0.9, blocks the doorway), Storage basket kind (6 fiber + 2 cordage, 12 stacks, BasketPut/BasketTake, G opens a panel), Weathering.cs (StructureTouch, GroundItemTouch, hourly sweep, darken and sag in the last day). BuildTest covers door and basket; passes locally in 196 s.
- 2026-10-15 23:40: rebased on #10 (StructureProgress, Use registry). Locks (D-048): StructureDoor has Locked and HungBy; ToggleLock for whoever hung it or claim members (Claims' ClaimMemberHook); a barred door opens only for them and shows a pale bar. Door and basket are the `building.use` Use handler (priority 28), Shift+G bars. Publishing over #10's schema needs no wipe (checked). BuildTest covers barring; passes in 200 s. Screenshots: building-06-door-basket.png, building-07-barred-door.png.
- 2026-10-15 23:55: rebased on #10 with Claims #8; Basket is StructureKind 3. Weathering and door bars use Claims' ClaimAt/HasClaimRights directly (no new hooks); Drop in Reducers.cs calls OnPlayerDroppedItem (2 lines). Weathering checked with shortened timers: an untouched lean-to outside a claim was gone after the sweep. BuildTest passes (184 s).

## Open questions / blockers
- Shift+G for barring is a default; asked the PM/UX whether a different key or gesture is wanted.
- Ceramics' Kiln appends after Basket (4).

## Next step
After #10 merges: reset claude/house-building-8ueqz2 to main, cherry-pick the local building-3 commits (backup: /mnt/project-files/patches/building-3.patch), regenerate bindings, run BuildTest, push and open the building-3 PR.
