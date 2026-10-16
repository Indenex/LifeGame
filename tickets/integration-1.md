# integration-1: Take over integration, CI and builds from Crafting
Lane: Integration
Owner: Integration session session_01Rf5965nk4Lvd3f2QDteZSq
Status: in_progress
Branch: claude/integration-lane (harness assigned none; session made this one)
PR: (once opened)

## Goal
Per D-058 the Integration lane, not Crafting, owns integration, CI and builds. Done means: the lane knows and has checked the shared files (`client/scripts/Main.cs`, `client/scripts/Hud.cs` hooks, `server/spacetimedb/Tables.cs`, `shared/World.cs`), `.github/workflows/build.yml`, `tools/gen-bindings.sh` and the Use dispatcher (D-025); `main` builds green in CI; open PRs listed under "Next merges" in `STATUS.md` are checked for conflicts with `main` and each owning lane has been told what blocks it. Justin sees no change in the game; he sees PRs reaching green faster.

## Progress log
- 2026-10-16: ticket written by the PM (D-058).
- 2026-10-16 16:50: Integration session started on `claude/integration-lane`. Installed .NET 8.0.426 and SpacetimeDB 2.12.0 locally (no Godot yet).
- 2026-10-16 16:50: last finished CI on main: run 340 on ca1ce9f, green (https://github.com/Indenex/LifeGame/actions/runs/38860380562). Runs 343 (84f44d2) and 345 (217dc13, docs-only PM merges) in progress.
- 2026-10-16 16:50: test-merged all 11 open PRs into main 217dc13 (`git merge-tree`). Text-clean: #77, #76, #75, #73, #71, #66, #61, #47, #43, #35. Conflict: #70 crafting-chop-wood in `client/scripts/crafting/CraftingUi.cs`. `STATUS.md` "Next merges" list is stale (#10, #8, #23, #21, #26, #27, #25, #28 no longer open).
- 2026-10-16 17:10: all 11 open PRs green in CI on their own heads (3 checks each), but most were based on old main. Built each merged onto main 217dc13 locally (server module, bindings, client `dotnet build`, all tools): #77, #76, #75, #73, #71, #66 build with 0 errors.
- 2026-10-16 17:15: #70 crafting-9: merged main into `crafting-chop-wood`, resolved `CraftingUi.cs` (kept main's `reopenAfterMinigame` field and the branch's `Start(ProcessDef)`), local build green, pushed 546afd3.
- 2026-10-16 17:20: all 11 PRs merged together onto main 217dc13 (Items.cs #61 x #70 resolved by keeping both lines): server, client and all tools build; local server ran LoadTest 5x5 s, BuildTest, SpawnCheck, CraftCheck, SocialTest and ClayCheck, all passed. No runtime breakage between lanes.
- 2026-10-16 17:20: Claims (claims-7) asked about Main.cs hooks and a Use-on-player handler: OK'd the 3-line Main.cs edit in its own PR; `claims.trade` uses `Use.Register` with a Target at Priority 5 (world handlers are 10-26), no shared helper yet.
- 2026-10-16 17:25: main moved to d0caf26 (#76, #77 merged). Latest finished main CI: run 370 on 4a2cd49, green (https://github.com/Indenex/LifeGame/actions/runs/38937047510). #70 now 95c123f, CI green (run 364).
- 2026-10-16 17:25: survey vs main d0caf26: #43 and #61 conflict with main only in `STATUS.md` (lanes edited it on their branches). Cross-PR conflicts: #61 x #70 `shared/Items.cs` (adjacent ItemDef lines, keep both; no id clash: Log 15, SleepingMat 130); #70 x #92 `client/scripts/net/Subscriptions.cs`, `tools/CraftCheck/Program.cs`; #92 x #47 `client/scripts/crafting/FireView.cs` (both Crafting-lane). Others clean: #35, #47, #66, #71, #73, #75, #90.
- 2026-10-16 17:35: opened PR #95 (this ticket and STATUS row). Merged main into #43 (8183eac) and #61 (ee769b3), taking main's STATUS.md; both build locally. Told Journal, House building and Crafting lanes; sent the PM the readiness list and a STATUS.md-on-branches question.
- 2026-10-16 17:25: PM (D-073): `client/scripts/Avatar.cs` moves to the Animation lane (session_019roNRWQBPw4ozNLDhzseig); the Modeling lane (session_01JKX3FNjkrEkAqoTaFJiXAm) may add `client/assets/models/` glTF and will need import settings and CI help from this lane.
- 2026-10-16 17:25: Atmosphere (atmosphere-12, D-072) adds a public `craft_work` table in WorkSounds.cs. Advised: Crafting declares and calls partial `OnCraftStarted`/`OnCraftEnded` in Crafting.cs now, so there's no merge-order dependency; clear rows on disconnect and abandoned jobs. PR #95 CI run 38939185639 queued.
- 2026-10-16 17:26: Crafting merges main into whichever of #70/#92/#47/#97 goes second; Integration keeps #61 x #70. Merge order sent to the PM: #97 (crafting-12 hook declarations) before atmosphere-12 (two defining declarations of one partial method = CS0756); Atmosphere drops its temporary declarations in its merge-main commit.
- 2026-10-16 17:30: Animation (anim-1, draft PR #100, `claude/anim-1-player-animation`) adds one Main.cs line after `crafting.Attach(conn);`; OK'd, merges clean with every open branch. Main.cs survey: claims-7 (`claude/claims-7-hand-item`, no PR yet) x #73 conflict on the `Panels.Open`/`claimPanel` block; resolution = claims-7's lines plus #73's `Panels.Side = () => crafting.PanelOpen;` and `|| Panels.Side()` in `useKey.Blocked`. Integration fixes whichever merges second.
- 2026-10-16 17:40: PM merged #75, #70, #47, #73, #71, #95 (main 0477696) and adopted the STATUS rule (lanes never edit STATUS.md on branches). Merged main into #61 (`shared/Items.cs`: kept Log and SleepingMat), built, ran BuildTest/SpawnCheck/CraftCheck locally (pass), pushed fb3e604. Survey vs main bdeaf3b: all 12 open PRs clean, no pairwise conflicts; claims-7 (no PR) conflicts in Main.cs with merged #73, resolution sent to the Claims lane.
- 2026-10-16 17:40: CI backlog: 7 main runs queued at once (every merge runs the full 20-bot job). Added a `concurrency` group to build.yml: PR runs cancel superseded runs of the same PR; on main the running build finishes and only the newest queued build waits.
- 2026-10-16 18:05: PM delegated merges (D-095). Merged #107 (dbf4dff, CI concurrency) and #97 (927e22a, local merge build passed). Then this session's permission check blocked merge work ("merge without review", the delegation came from the PM, not Justin), so merges are back with the PM; lanes send green shas to the PM. #121 (integration-2, sneak flag) opened.
- 2026-10-16 18:15: check-in survey vs main cb84d64: all 20 open PRs clean vs main. Pairwise conflicts, whoever merges second merges main in: bugfix #99 x #108, #108 x #109 (CeramicsView.cs); #105 x #108, #105 x #109 (ceramics); #105 x #61 (Building). Sent to the PM.
- 2026-10-16 18:20: correction: #110 x #111 was a false positive (survey modeled the first merge with one parent; #111 contains #110). Pairwise check now uses `git commit-tree <tree> -p origin/main -p origin/<first>`. Other conflicts confirmed.
- 2026-10-16 18:40: Justin's merge permission for this session not in place (PM's session refused to add the rule; Justin adds it himself), so the PM keeps merging; this lane sends green shas. #121 green on 777ff5e, sent. Survey vs 235fe50: all 16 open PRs clean; only #108 x #105 conflict (ceramics).
- 2026-10-16 18:42: D-129: exactly one world wipe, with Environment's env-2 land reshape, on a date Justin confirms. This lane helps Environment with `shared/World.cs` and the upgrade check, and flags any other PR that would wipe.
- 2026-10-16 18:48: prepared option 1 on `claude/integration-wipe-label` (not a PR yet): `world-wipe` label skips the upgrade check and prints a warning; `pull_request` also triggers on labeled/unlabeled.
- 2026-10-16 18:42: #121 (integration-2, sneak flag) merged by the PM; Animation told #100 can follow after Justin sees the clips.

## Open questions / blockers
- Sent to PM 18:45: how env-2's intended wipe passes CI's upgrade check (options in the message).

## Next step
After each merge to main, re-run the conflict survey (`git merge-tree` each open PR vs main and pairwise) and send the PM which PRs need main merged in; get #121 (integration-2) green and to the PM.
