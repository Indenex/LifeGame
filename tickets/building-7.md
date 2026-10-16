# building-7: Playtest fixes for laying out and building
Lane: House building
Owner: House building thread
Status: done
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

- 2026-10-16: #61 merged (green fb3e604). Justin picked option A of the D-075 shelter options as D-116; it ships in building-8.

## Open questions / blockers
- Next building ticket sent to the PM 2026-10-16 (default: fence and gate). Note for Ceramics: the jar's stage costs now go in `Structures.StageCosts` in shared/Structures.cs, not Building.cs.

- Asked the PM for the next ticket (building-4 already merged as #53): fence and gate (recommended), raised hearth, or log cabin. Default: fence as building-8 after #61 is green.

- Ceramics (ceramics-6, D-066) adds StructureKind.StorageJar = 6 using the basket store; told them to put its cost row in Structures.StageCosts after #61. Building takes kinds 7 and up.

- PM: D-075 (start low, grow slowly). After #61, review Building's numbers and send options. Draft (shelter Warmth = share of the cold drain removed; open sky drains full warmth in 16 min when mild, 8 on a cold night):
  - Now: lean-to rain 0.8 / warmth 0.3; hut 1.0 / 0.75 (door shut 0.9); house 1.0 / 0.85 (door 0.9, fire inside 1.0).
  - A (recommended): lean-to 0.6 / 0.2; hut 0.9 / 0.4 (door 0.5); house 1.0 / 0.5 (door 0.55); a fire inside still makes it fully warm. Hut on a cold night: 13 min instead of 32, so fires matter.
  - B: trim only: lean-to 0.8 / 0.25; hut 1.0 / 0.6 (door 0.7); house 1.0 / 0.7 (door 0.75).
  - C: keep (house numbers were Justin's D-057).
- Environment lane (session_012hVejTxLoxBpZyX3GQmNL9) builds around buildings; Modeling lane (session_01JKX3FNjkrEkAqoTaFJiXAm) may upgrade StructureModel meshes and will say first.

## Next step
Done (merged as #61).
