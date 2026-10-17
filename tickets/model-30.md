# model-30: Heath variety: heather, gorse and birch (D-166)
Lane: Modeling
Owner: Modeling lane (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: done
Branch: claude/model-30-heath-variety
PR: #766

## Goal
Heather, gorse and birch each had 3 variants, so heath patches repeated them often. Each goes to 6 (`PropMeshes.BirchVariants/HeatherVariants/GorseVariants`). Heather 3 to 5 grow into two lobes, the second smaller. Gorse 3 to 5 repeat round, tall and sprawling with their proportions pushed about and a lean to one side. Birch 3 to 5 are new seeds of the same slim trunk: bend, slimness, bark marks and the dark foot all differ. Terrain and ChunkWorker loop over the counts, so callers pick them up with no change. Showcase view `heath`.

## Progress log
- 2026-10-17 12:25: built and rendered (renders/model-30-heath.png); local build 0 errors.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the green sha to the PM.
