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

## Open questions / blockers
- Sent to PM 17:35: lanes stop editing STATUS.md on their branches (recommended) / lanes resolve each time / PM resolves at merge. Default meanwhile: option 1.

## Next step
After each merge to main, re-run the conflict survey (scratchpad script logic: `git merge-tree` each open PR vs main and pairwise) and merge main into whichever of #61 / #70 merges second, resolving `shared/Items.cs` by keeping both lines.
