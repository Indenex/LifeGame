# building-19: Sundial (the PM's "building-18"; building-18 was already Shift keeps placing)
Lane: House building
Owner: House building thread (Sonnet)
Status: done
Branch: claude/house-building-8ueqz2-sundial
PR: #638 (merged)

## Goal
Justin: "a sundial item, and when it's built, a little curved time-of-day UI thing at the top of the screen that tracks the time of day and moves with day and night." Access (Justin): "everyone part of the claim gets it." So every member of a claim with a standing finished sundial, and the rest of that claim's town, has the dial.

## Progress log
- 2026-10-17: Sundial StructureDef (data, D-206) in shared/structures/StructuresHouse.cs: Category Crafting, claim-only (not Wild), round r 0.4 m, solid, two stages (Plinth: 3 stone; Dial: 1 stone + 1 stick), `Sundial` flag (this kind keeps the time) and `FixedYaw` flag (always laid out facing world north; server forces yaw 0 and the client ghost does not turn, so the marks read true). StageProcess: stone work, framing. `StructureKind.Sundial` appended after StoneHearth; Kind = 20 for now (the PM's plan is 23 after the devices 20-22; the startup check catches a mismatch, so renumber when #285 lands).
- Server (D-182): `server/spacetimedb/BuildingSundial.cs` public per-player view `my_sundial` (`MySundial`, rows of `SundialAccess { ClaimId }`): a row for each claim the player owns or belongs to that has a finished sundial inside its radius, or whose town roster has such a claim. The client shows the arc while the view is non-empty. No schema change (a view only).
- Tests: BuildTest `sundial` (also `both`) builds one with two players in a claim and checks owner and member get a row and a stranger does not; passes locally (43 s). The town path is the same code but is not exercised by a test yet.
- Model: Modeling's sundial model is merged in.
- 2026-10-17 11:22: #638 merged (CI run 39027575978 green). Kind 20 is final (PM: Crafting renumbers its devices to 21-23). The town path of `my_sundial` had no test.
- 2026-10-17 11:50: BuildTest `sundial` now also covers the town path (building-21): B raises a second claim 42-58 m from A's (new `Bot.FindRingSite`), A starts a town and invites B, B signs, and B gets a `my_sundial` row with no membership of A's claim; passes locally (58 s).

- 2026-10-17 11:25: Render requested 02e86586 (Render B): sundial still (--view=sundial) and the arc in game, for Justin; PM asked.

## Open questions / blockers
- (none)
- Journal 'Things to try' line sent to the PM: "A flat stone and a stick, set in your claim, will tell the whole claim the hour."

## Next step
Wait for CI on the town-path test PR, then send the green sha to the PM.
