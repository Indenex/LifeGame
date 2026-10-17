# survival-31: The server keeps its own stamina bar
Lane: Survival
Owner: Survival thread
Status: in_review
Branch: claude/survival-harden
PR: #826 (also carries survival-32, the straw-bed prompt in RestSpotView)

## Goal
Closes the last client-only survival gap from the D-182 audit. The client still predicts running, but the server now tracks a stamina bar of its own (`server/spacetimedb/StaminaServer.cs`, private table `StaminaBar`, one hook line in `Move`), fed by how far the player really moved. A client that sprints forever is held to the rhythm the bar allows; an honest run and rest is never slowed. Running dry only caps pace at walking (`Stamina.WalkPace`, 4.2 m/s plus 10%); nothing else is taken (D-081). Players see no change.

How it works: a recent average of pace (meters and seconds, both fading over `Stamina.ServerAverageSeconds` = 2 s), so a lag spike that delivers several steps at once never counts as sprinting. Pace above `WalkPace` counts as running by its share of the run speed. The bar drains at `ServerDrainFactor` 0.8 of the client's rate (the server runs dry 25% later), refills as the client's does (`Stamina.Step`, rank and good-meal bonus included), and winded clears at `MinToRun` like the client. While winded, a step is cut back so the average stays at or under `WalkPace`. The server does not charge jumps (the client does); that only adds to the margin.

## Progress log
- 2026-10-17 13:00: built on `claude/survival-harden` after bots moved to 4 m/s (#748). SurvivalCheck gained two cases: a sprint at 5.9 m/s for 32 s is held to under 5.4 m/s over its last 20 s (a free sprint covers 118 m; capped runs cover 88 to 105 m), and a 5 s run, 12 s rest, 5 s run covers at least 90% of what two free 5 s runs would. Upgrade check passes.
- Why the cap is gentle: the client's own rules already let a held run key run about 40% of the time once winded (it refills to 0.2 and runs again). The server allows the same plus the margin, so a hacked client gains well under a metre a second over an honest one.

- 2026-10-17 14:50: merged main (#748, #776, #781), opened #826. Local: SurvivalCheck, CharacterCheck, SpawnCheck, SocialTest, CraftCheck, ClayCheck, upgrade check, ctx-sender gate pass; client builds. BuildTest showed a "no clay" bot-distance warning and a FAIL at the Sundial gather step, with and without the stamina hook, so I judge it not mine; CI is the verdict. survival-32: RestSpotView offers any finished building whose kind is a RestSpotDef (straw bed Id 2), untested in play, compiles.

## Open questions / blockers
- Waiting on CI for #826. Nothing waits on Justin; tell him the cap is gentle by design (the client's own rules already let a held run key run ~40% of the time once winded; the server allows that plus 25%).

## Next step
Check CI on #826 (indenex/lifegame). If green, send the head sha to the PM to merge. If BuildTest fails at the Sundial step, compare with main's CI before touching anything. Then try the straw bed in play (build it in a claim, Use it: expect the waking-place prompt and Lie down).
