# ceramics-10: Clay roof tiles
Lane: Clay and ceramics
Owner: Clay and ceramics lane (Sonnet session_01Sc3N1RCsurSszfX5YwGzMU holds it now)
Status: in_progress
Branch: claude/clay-ceramics-10b-kfr4kr (replayed onto main after the lamp PR merged as a squash; the older claude/clay-ceramics-10-kfr4kr is superseded)
PR: (none yet)

## Goal
Justin picked roof tiles (2026-10-16). Fired clay tiles are a new roofing material for a house: they last far longer than thatch and keep the rain out. Done when a player can shape tiles from tempered clay, fire them in a kiln (pit fires crack them, like the storage jar), and put them on a house as roofing, with a tile look on the roof. Per docs/CONTENT.md the house side is StructureDef data (a Shapes and StageCosts row plus a model), and tile making is a ClayForm plus a ProcessDef, with no new switch.

## Progress log
- 2026-10-16 22:30: Justin picked roof tiles via the PM. Ticket opened. Asking House building (session_01DD9p6EhSc5pJ8CjNmDFTrG) how tiles go onto roofs; options in Open questions.

- 2026-10-16 22:50: House building agreed option 1 (Tiled house kind); they add the Shapes row, costs and model (after their building-12 folds StageCosts into the shape); I give them Item.RoofTile (121). Built: ClayForm.Tile (kiln-only), items 120 Unfired Roof Tile / 121 Roof Tile, clay.tile at the bench (1 tempered clay -> 1 tile; 12 tiles on a roof is the same clay as 24 at 2 per clay; a kiln holds 6), curved-slab mesh and looks, campfire refusal names the piece, Journal line (clay.tile needs bench + kiln). ClayCheck --tile PASSED (c10tile2). Numbers sent to the PM as options (tile count, weathering hours); not answered yet. Kiln firing of tiles goes through the same path as the jar (--kiln --jar).

- 2026-10-16 23:05: Justin decided (via the PM): 12 tiles per roof. No custom weathering value: house-type buildings, including the Tiled house, can only be built inside a claim and anything outside a claim weathers fast (48 h untouched); the Tiled house follows the same claim rules as the brick house (House building's building-13 adds the claim-only flag). The 504 h idea is dropped. Told House building.

- 2026-10-17 00:00: #343 (tiles) and #356 (tallow in the pot meshes) merged. ceramics-12 (branch claude/clay-ceramics-12-kfr4kr): ClayCheck --kiln --jar now also presses a roof tile, dries it, a campfire refuses it, and it comes out of the kiln as a fired Roof tile (c12kt3 PASSED, tile quality 80). Test-only change. After House building's #362 (kiln and bench claim-only) lands, re-run --kiln --jar to confirm the kiln is built inside the claim.

## Open questions / blockers
Sent to House building and the PM, 2026-10-16 22:30. How do tiles get on a roof? Working on option A until told otherwise.
1. (Recommended) A new house kind, "Tiled house": a brick house whose last stage is "Roof tiles" (tile cost) instead of thatch. Pure data, no world change; players choose thatch or tile when they lay out the house. Cost: two house kinds that share most of their shape.
2. A separate "Tile roof" structure laid over a finished brick house or clay hut as an upgrade: players with existing houses can re-roof. Cost: an overlay structure that needs a new rule (it sits on a home), more code.
3. Add "Roof tiles" as an optional extra stage on the existing brick house: no new kind. Cost: it changes the existing kind's stage count, which touches stored houses.
DECIDED 2026-10-16 (Justin): 12 tiles per roof, no custom weathering (claim rules). Original proposal, now superseded: (start low, grow slowly, D-075): 1 tempered clay shapes 2 tiles (flat slab, no minigame, at the Potter's bench), kiln-fired only; a tiled house takes 24 tiles on the roof stage in place of 8 fiber of thatch (now planned as 12 tiles, 1 clay each, the same clay); it weathers after 504 h untouched (brick house 168 h).

## Next step
When the lamp PR #309 merges, merge main into claude/clay-ceramics-10-kfr4kr, run ClayCheck --tile and tools/upgrade-check.sh origin/main, open the PR and send the PM the green sha. Then (a) done: House building told 12 tiles, no custom weathering, (b) extend ClayCheck to fire tiles in the kiln run. Older text follows.
Wait for House building's answer on how tiles attach (option 1 unless told otherwise); meanwhile add ClayForm.Tile (append after Lamp), items for an unfired and a fired tile (ids from the Ceramics range, next free after 119), and clay.tile at the bench.
