# integration-19: Test-only speed-up for the bot checks (ClayCheck in about 30 s)
Lane: Integration
Owner: Integration thread (session_01XqJiErC6c8P4DJBSQNi1Br)
Status: done
Branch: claude/integration-vflv1x
PR: #838

## Goal
Game times stay as they are. Only the bot checks run faster (Justin, PM thread, 2026-10-17 15:06: "can we just shorten the test/duration for testing only?", then "make it take like, 30s"). The server has a private, admin-only `TestSpeedConfig` (`server/spacetimedb/TestSpeed.cs`, reducer `set_test_speed`, factor 1 to 60, default 1). Players' worlds never set it. It divides clay drying (and hide drying, which shares `DryRateAt`), pit and kiln firing, and craft times. Fires and roasts keep their real times. The fuel a firing needs is counted in firing time, so the same fuel covers a shorter firing. Plain ClayCheck sets x60 itself through spacetimedb-cli as the database owner. The other modes keep real times unless given `--speed=N`.

Step 2, for the 30 s target: the run spends about 3.5 min on walking, digging 12 clay and building the claim and bench. That needs an admin test kit (seed the bot's inventory and a bench) through a second admin reducer.

## Progress log
- 2026-10-17 15:20: step 1 written. Hooks are in Ceramics.cs (DryRateAt, StartFiring, FireCanTakePot, RequireFuelForFiring), Kiln.cs (firing length and fuel) and Crafting.cs (job seconds). ClayCheck now lights the fire first and shapes the damp pot alone, firing it at once. Sped up, a wet pot dries in seconds, so shaping both pots first would leave nothing damp to test. Not built locally: this container can't reach the .NET download, so CI builds it.

- 2026-10-17 15:25: nightly run 39044128357 on 5d29c8b passed. The bot's run took 2.5 min (it was 12.8), and the whole job 3.5 min. Timeline: gathering 0.6 min, digging 0.2, axe and log 0.1, claim 0.2, bench 0.1, tempering 0.1, fire and damp pot 0.2, drying 5 s, firing 4 s, then the walk to the water and back 0.8 min.

- 2026-10-17 15:45: #838 merged by the PM (green at c0046b7). Justin approved the test kit (PM thread card, 15:28). This session's guard wouldn't act on a relayed tap, so the PM sent it to Bug fixes, who built it on claude/bugfix-rnwmoa-8 (2c11f72a) and will open its own PR.

## Open questions / blockers
- None.

## Next step
None for this ticket. The kit PR belongs to Bug fixes, and Integration checks its nightly time when it lands.
