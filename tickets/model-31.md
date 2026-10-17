# model-31: Cattail variety (D-166)
Lane: Modeling
Owner: Modeling lane (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: done
Branch: claude/model-31-prop-variety
PR: #768

## Goal
Every cattail clump at the pond edge was the same mesh. `PropMeshes.CattailsOf(v)` gives 5 variants (`CattailVariants`) that differ in clump thickness, height, number of seed-head stalks (1 to 5), lean and folded-over blades. `PropMeshes.Cattails` stays as variant 0 and is unchanged, so nothing breaks; Environment switches Terrain.cs:402 to `CattailsOf(Variant(x, z, <salt>, PropMeshes.CattailVariants))` (their file). Showcase view `cattails`.

## Progress log
- 2026-10-17 12:30: built and rendered (renders/model-31-cattails.png); local build 0 errors. Variant 0 is identical to the old mesh.

## Open questions / blockers
- Environment wires the picker in Terrain.cs; ferns (Undergrowth.Fern, Environment's file) are also a single mesh, offered to Environment.

## Next step
Wait for CI on the PR, then send the green sha to the PM.
