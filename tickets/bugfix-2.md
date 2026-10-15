# bugfix-2: BuildTest clay-hut gathering times out
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: done
Branch: claude/bugfix-lyle6o (ticket only; no code change needed)
PR: none (fixed on main by 361a6c7)

## Goal
Find out why "both gathered materials for the Clay hut" timed out on the claims branch at 22988d7 (run https://github.com/Indenex/LifeGame/actions/runs/38816547724) while the same code passed at d367547, and fix it or show it is fixed on main.

## Progress log
- 2026-10-15 23:00: root cause. The failing run stopped at Clay:11 of 12. BuildTest's old clay loop gave up on a cell at depth MaxDigDepth-1 (11) without digging it, but NearestClay did not skip that cell, so the bot picked the same cell again forever and never got the 12th lump. It only bit when one cell supplied 11 lumps in a row, which depends on where the bot stood, hence pass/fail between runs. 361a6c7 ("crafting-ci: BuildTest skips dug-out clay", on main since 22:15) digs until MaxDigDepth and skips only fully dug cells; 22988d7 and d367547 both predate it.
- Reproduced locally: 22988d7's BuildTest after a 5-bot LoadTest fails with Clay:11; main's BuildTest passed 3 of 3 the same way (Clay:12, 55-64 s). CI on main-based PR #18 is green too.

## Open questions / blockers
- (none)

## Next step
None. Claims (or any branch) gets the fix by merging main.
