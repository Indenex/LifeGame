# building-3: Kiln support, hut door, storage basket
Lane: House building
Owner: House building thread
Status: done
Branch: claude/house-building-8ueqz2 (restarted from main 58d50e0 after #10 merged)
PR: #41

## Goal
Ceramics can place an updraft kiln from the B menu as a blueprint (D-035): Building provides the Solid round-shape flag and the CeramicsStructureRemoved hook (done in #10). A finished clay hut can get a woven hurdle door that G opens and closes; closed, it keeps more warmth. A coiled-fiber storage basket can be placed from the B menu and holds items that G opens.

## Progress log
- 2026-10-15 22:40: kiln hooks landed in #10 (e43c0f3). Design questions on the door and basket sent to the PM.

- 2026-10-15 23:00: Justin's answers (D-046): anyone opens doors; baskets open to anyone except in a claim (members only); player-made things outside claims weather after 72 h untouched, gone 24 h later. Built: HangDoor/ToggleDoor (closed door: warmth 0.9, blocks the doorway), Storage basket kind (6 fiber + 2 cordage, 12 stacks, BasketPut/BasketTake, G opens a panel), Weathering.cs (StructureTouch, GroundItemTouch, hourly sweep, darken and sag in the last day). BuildTest covers door and basket; passes locally in 196 s.
- 2026-10-15 23:40: rebased on #10 (StructureProgress, Use registry). Locks (D-048): StructureDoor has Locked and HungBy; ToggleLock for whoever hung it or claim members (Claims' ClaimMemberHook); a barred door opens only for them and shows a pale bar. Door and basket are the `building.use` Use handler (priority 28), Shift+G bars. Publishing over #10's schema needs no wipe (checked). BuildTest covers barring; passes in 200 s. Screenshots: building-06-door-basket.png, building-07-barred-door.png.
- 2026-10-15 23:55: rebased on #10 with Claims #8; Basket is StructureKind 3. Weathering and door bars use Claims' ClaimAt/HasClaimRights directly (no new hooks); Drop in Reducers.cs calls OnPlayerDroppedItem (2 lines). Weathering checked with shortened timers: an untouched lean-to outside a claim was gone after the sweep. BuildTest passes (184 s).
- 2026-10-16 00:05: restarted from main 58d50e0 (#10 and Claims #31 in). Weathering uses Claims' InClaimHook. Hanging a door reports build.door to skills. Fixed main's build: Kiln.cs's CeramicsStructureRemoved is now the partial implementation (main failed CS0111), and ClayRules.KilnStructureKind is 4 (Basket is 3, agreed with Ceramics). LoadTest, CraftCheck and BuildTest pass (193 s); publishing over the pre-#10 schema needs no wipe.

- 2026-10-16 00:25: merged main cb9d155 (#40 kiln is StructureKind 3, its Kiln.cs and ClayRules.cs taken as is); basket is now StructureKind 4. Local: LoadTest 5x5, SpawnCheck, CraftCheck, BuildTest (200 s), SocialTest and upgrade-check over main all pass.
- 2026-10-16 00:45: merged main ef1c26b (UX #38). Door, basket and blueprint Use offers now pass Target, so the thing you face wins. Local LoadTest, CraftCheck, BuildTest and upgrade-check pass.

## Open questions / blockers
- Shift+G for barring is a default; asked the PM/UX whether a different key or gesture is wanted.
- Ceramics' Kiln appends after Basket (4).

## Next step
Done: merged as #41 (88994fa).
