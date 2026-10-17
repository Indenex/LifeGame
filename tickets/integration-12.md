# integration-12: One shared bot walk; CI checks cannot fail from a stuck bot
Lane: Integration
Owner: Integration thread
Status: in_review
Branch: claude/integration-botwalk
PR: (to be filled in when opened)

## Goal
Justin: "Can we make it where CI tests can't fail from a stuck bot?" Not fewer checks: bot movement in the test tools is robust, so a check fails only on the thing it tests. All check tools walk through one helper, `tools/common/BotWalker.cs`: route-find round trunks, boulders and walls (BotGround), watch the SERVER's position for a stall (under 0.3 m in 3 s), sidestep and re-route twice, then reposition the bot at its target with the new admin-only `admin_teleport(x, z)` reducer and log a `::warning::` (tool, bot, stuck position, what is near) so the stuck spot still gets seen and fixed. Walking itself is tested in exactly one place: SpawnCheck's water crossing uses a strict walker (no reposition; a stall fails).

## Progress log
- 2026-10-17: server `AdminTeleport` (Admin.cs, `AdminRules.TeleportMax`): IsWorldAdmin only, finite and bounded, clears the pending move, writes PlayerPos. No table, so no wipe. SocialTest checks that a non-admin is refused, an admin is moved, a NaN target is refused, and that a forced stall (StallMetres set to 1000) takes the fallback and logs the warning.
- 2026-10-17: BotWalker written; CraftCheck, ClayCheck, BuildTest (scenario walks and the gather loop's stall watchdog), SocialTest and SpawnCheck (strict) moved onto it. Each makes its bot an admin through the CLI (`make_admin` by name; a bot whose name is shared is renamed first).
- Failure census (last 50 failed Build runs, 85 failed jobs): bot stuck while walking 18 runs (CraftCheck "stuck walking" 5, "position to settle" 4, "Out of reach" 3; BuildTest clay hut "B: Out of reach" 5; SocialTest stuck leg 1): covered by the stall detector + reposition. Position-dependent 24 runs: BuildTest material gathering from a bad start (11; covered by the gather watchdog and the origin start of bugfix-31/#436), CraftCheck fire spot (7: boulder under the fire, no level ground; NOT a walk problem, needs CraftCheck to pick another spot: separate ticket for Crafting), claim rules vs the spawn clearing (4: partly fixed by starting at the origin), SocialTest cooldown/reach (2). Other 8: old `dotnet publish` compile errors, not bots.

## Open questions / blockers
- Not covered by this PR: CraftCheck fire placement on a boulder or without level ground (7 runs). That is a "choose another spot" fix in CraftCheck's fire step, owned by Crafting/Bug fixes.
- Survival's SurvivalCheck (#546) should use BotWalker when it lands (their `WalkTo`).

## Next step
Wait for CI on the PR, then send the PM the green sha.
