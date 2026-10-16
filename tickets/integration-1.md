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

## Open questions / blockers
- (none)

## Next step
Clone, run `tools/gen-bindings.sh`, build server and client locally, read the latest CI run on `main` and record its link and result here.
