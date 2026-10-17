# model-32: Fern variety (D-166)
Lane: Modeling
Owner: Modeling lane (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/model-32-fern-variety
PR: (opening)

## Goal
Every fern on the woodland floor was the one `Undergrowth.Fern` mesh. `Undergrowth.FernOf(v)` (in Environment's file, a small hook offered to them) gives 5 variants (`FernVariants`) differing in frond count (5 to 10), reach, arch, droop and frond breadth, some with one or two curled fiddleheads. `Undergrowth.Fern` stays variant 0, identical to before. Environment splits Terrain.cs's one fern `data.Add` into per-variant batches (their file). Showcase view `ferns`.

## Progress log
- 2026-10-17 12:40: built and rendered (renders/model-32-ferns.png); local build 0 errors.

## Open questions / blockers
- Environment to agree to the hook in Undergrowth.cs and wire Terrain.cs.

## Next step
Wait for CI and Environment's reply; then send the green sha to the PM.
