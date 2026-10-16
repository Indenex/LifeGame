# building-7: Playtest fixes for laying out and building
Lane: House building
Owner: House building thread
Status: in_progress
Branch: claude/house-building-8ueqz2 (riding on #61 with the mat, per the PM)
PR: #61

## Goal
The PM's playtest 2 findings (/mnt/project-files/screenshots/playtest2-*). Costs are unchanged.
1. The layout hint lists each stage's cost: "Needs 4 sticks, then 3 plant fiber, then 6 sticks, then 8 plant fiber".
2. Standing at a site while carrying none of what it still needs adds "Needs 4 more plant fiber" under the progress line.
3. The layout outline draws over trees and trunks (no depth test), so it never hides behind one. Trees inside it are cleared once it is laid out.
4. The thatch underside is wound both ways, so a finished lean-to seen from the open (ridge) side shows its roof, not a bare pole on posts.

## Progress log
- 2026-10-16 05:10: all four done. Stage costs moved from Building.cs to `shared/Structures.cs` (`Structures.StageCosts`) so the client can show them; the server reads the same table. New showcases `--building-leanto` and `--building-placing`. Screenshots: /mnt/project-files/screenshots/building-12-lean-to-both-sides.png, building-13-layout-costs.png. Patch: /mnt/project-files/patches/building-7/.

- 2026-10-16 05:36: CI green on 56d830c (run https://github.com/Indenex/LifeGame/actions/runs/38855592389).
- 2026-10-16: D-061, Justin keeps the mat, so #61 merges with building-5 as is. Merged main in (clean, no conflicts); all local checks green. Pushed for a fresh CI run.

## Open questions / blockers
- Next building ticket sent to the PM 2026-10-16 (default: fence and gate). Note for Ceramics: the jar's stage costs now go in `Structures.StageCosts` in shared/Structures.cs, not Building.cs.

- Asked the PM for the next ticket (building-4 already merged as #53): fence and gate (recommended), raised hearth, or log cabin. Default: fence as building-8 after #61 is green.

## Next step
Wait for CI on #61's new head, then send the PM (session_01CvoSQGRe3vDvDXWQu2qruU) the green sha.
