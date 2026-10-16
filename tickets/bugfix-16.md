# bugfix-16: SocialTest fails locally unless newcomer grace is 0
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-lyle6o
PR: #77

## Goal
`cd tools/SocialTest && dotnet run -- http://127.0.0.1:3000 <db> [1]` passes on a freshly published database with no setup, just as it does in CI.

Cause: the fighting checks need newcomer grace 0, and the decay check needs a 30 s claim lifetime. Only CI's workflow step set them (as the database owner, through spacetimedb-cli), so a local run failed at "a strike in the wild lands". Fix: SocialTest calls `set_newcomer_grace 0`, and with decay=1 `set_claim_lifetime 30`, through spacetimedb-cli itself before its players join. Grace is fixed when a player joins, so the order matters. The CI step's own calls are now redundant but harmless, and are left alone (build.yml belongs to the integration owner).

## Progress log
- 2026-10-16: reported by Survival via the PM. Fixed; on a fresh local db with no pre-steps, `dotnet run -c Release -- http://127.0.0.1:3000 soc 1` printed "All social checks passed".

## Open questions / blockers
- (none)

## Next step
Wait for CI on #77, then send the PM the green sha.
