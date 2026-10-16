# crafting-11: Cold hearths and fires off the rocks
Lane: Crafting
Owner: Crafting thread
Status: in_review
Branch: crafting-cold-hearth
PR: #92

## Goal
From Bug fixes' playtest 5 (screenshots/playtest5-fire-gone.png, playtest5-fire-on-rock.png), via the PM:
1. A fire whose fuel runs out no longer vanishes. It leaves a cold hearth (the stone ring, grey ash, two charred stubs, a thread of smoke for the first few minutes) for an hour. Lighting a fire with a drill within reach of a cold hearth rekindles it in place. Players within 20 m hear "Your fire has burned down" (or "A fire nearby has burned down").
2. A new fire goes on the nearest clear, level spot to the one in front of the crafter: never on a boulder, against a trunk, in water, on a steep slope, or within 1.5 m of another fire or hearth. If nothing within about 1.75 m is clear, the craft is refused before anything is used up.

## Design
- New table `ColdHearth` (server/spacetimedb/Hearths.cs): adding a table needs no world wipe. `Fire` rows keep meaning "burning", so Ceramics, Cooking, Survival warmth and Building storage are unchanged.
- shared/Rocks.cs gives the server the client's boulder rule (Terrain.cs). It uses shared/Trees.cs, copied byte for byte from #70 (crafting-9) so the two merge cleanly.
- Numbers (Fires in shared/Processes.cs): ColdSeconds = 1 h, RingRadius = 0.6 m, Spacing = 1.5 m.

## Progress log
- 2026-10-16 06:00: started from main 456b0cf.
- 2026-10-16 06:25: server and client build. CraftCheck passes and now checks that no fire sits on a boulder. One run failed at a pond edge (ground 1 cm above the water), so the water margin is now 0.02 m.
- 2026-10-16 17:20: merged main 7c65c0f. Burnout tested locally with StartSeconds = 20 (not committed): both CraftCheck fires became cold_hearth rows with Until = +1 h, and the fire rows went. CraftCheck passes with real numbers; server and client build. Not tested by hand: rekindling at a hearth (FireSpot picks the hearth within reach; logic only) and the burned-down message (the bot was offline by then).
- 2026-10-16 17:30: PR #92 opened at 11ca4f8. Render requested 11ca4f8 (cold hearth via --screenshot-fire=cold, and a lit fire beside a boulder).
- 2026-10-16 17:40: 4689942 green (run https://github.com/Indenex/LifeGame/actions/runs/38938330662). #70 and #47 merged; merged main, both sides kept in Subscriptions.cs, CraftCheck and FireView.cs. Builds, and CraftCheck passes locally.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the merge with main (after #70 and #47), send the PM the green sha.
