# integration-12: One shared bot walk; CI checks cannot fail from a stuck bot
Lane: Integration
Owner: Integration thread
Status: in_review
Branch: claude/integration-botwalk
PR: (to be filled in when opened)

## Goal
Justin: "Can we make it where CI tests can't fail from a stuck bot?" Not fewer checks: bot movement in the test tools is robust, so a check fails only on the thing it tests. All check tools walk through one helper, `tools/common/BotWalker.cs`: route-find round trunks, boulders and walls (BotGround), watch the SERVER's position for a stall (under 0.3 m in 3 s), sidestep and re-route, then ask the server to shake the bot free with the player-facing `unstuck` reducer (Bug fixes, bugfix-44, PR #556: nearest free spot within about 12 m, 60 s cooldown, so at most one per bot per minute) and log a `::warning::` (tool, bot, stuck position, what is near) so the stuck spot still gets seen and fixed. Walking itself is tested in exactly one place: SpawnCheck's water crossing uses a strict walker (no reposition; a stall fails on the spot).

## Progress log
- 2026-10-17: PM decision: no admin_teleport; the last resort is Bug fixes' public `unstuck`. The helper finds `Reducers.Unstuck` by name, so it builds before #556 merges and the step simply stays off until then (a stuck walk fails after its sidesteps). Caveat: unstuck refuses when the server thinks the bot is not stuck (a bot pressed against a wall edge or trunk is standing on free ground), so wider sidesteps come first; after #556 merges, run the tools once to see how many walks still need it.
- 2026-10-17: BotWalker written; CraftCheck, ClayCheck, BuildTest (scenario walks and the gather loop's stall watchdog), SocialTest and SpawnCheck (strict) moved onto it. Bots do not need to be admins.
- Failure census (last 50 failed Build runs, 85 failed jobs): bot stuck while walking 18 runs (CraftCheck "stuck walking" 5, "position to settle" 4, "Out of reach" 3; BuildTest clay hut "B: Out of reach" 5; SocialTest stuck leg 1): covered by the stall detector + reposition. Position-dependent 24 runs: BuildTest material gathering from a bad start (11; covered by the gather watchdog and the origin start of bugfix-31/#436), CraftCheck fire spot (7: boulder under the fire, no level ground; NOT a walk problem, needs CraftCheck to pick another spot: separate ticket for Crafting), claim rules vs the spawn clearing (4: partly fixed by starting at the origin), SocialTest cooldown/reach (2). Other 8: old `dotnet publish` compile errors, not bots.

## Open questions / blockers
- Not covered by this PR: CraftCheck fire placement on a boulder or without level ground (7 runs). That is a "choose another spot" fix in CraftCheck's fire step, owned by Crafting/Bug fixes.
- Survival's SurvivalCheck (#546) should use BotWalker when it lands (their `WalkTo`).

## Next step
Wait for CI on the PR, then send the PM the green sha. After #556 merges, merge main in and confirm the unstuck step works (SocialTest is the place for a forced-stall check).
