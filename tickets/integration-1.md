# integration-1: Take over integration, CI and builds from Crafting
Lane: Integration
Owner: unassigned (PM starts an Opus session for it)
Status: todo
Branch: (record the harness-assigned branch here)
PR: (once opened)

## Goal
Per D-058 the Integration lane, not Crafting, owns integration, CI and builds. Done means: the lane knows and has checked the shared files (`client/scripts/Main.cs`, `client/scripts/Hud.cs` hooks, `server/spacetimedb/Tables.cs`, `shared/World.cs`), `.github/workflows/build.yml`, `tools/gen-bindings.sh` and the Use dispatcher (D-025); `main` builds green in CI; open PRs listed under "Next merges" in `STATUS.md` are checked for conflicts with `main` and each owning lane has been told what blocks it. Justin sees no change in the game; he sees PRs reaching green faster.

## Progress log
- 2026-10-16: ticket written by the PM (D-058).

## Open questions / blockers
- (none)

## Next step
Clone, run `tools/gen-bindings.sh`, build server and client locally, read the latest CI run on `main` and record its link and result here.
