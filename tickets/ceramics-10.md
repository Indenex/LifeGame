# ceramics-10: Clay roof tiles
Lane: Clay and ceramics
Owner: Clay and ceramics lane (Sonnet session_01Sc3N1RCsurSszfX5YwGzMU holds it now)
Status: in_progress
Branch: claude/clay-ceramics-10-kfr4kr (stacked on claude/clay-ceramics-9-kfr4kr until the lamp PR #309 merges)
PR: (none yet)

## Goal
Justin picked roof tiles (2026-10-16). Fired clay tiles are a new roofing material for a house: they last far longer than thatch and keep the rain out. Done when a player can shape tiles from tempered clay, fire them in a kiln (pit fires crack them, like the storage jar), and put them on a house as roofing, with a tile look on the roof. Per docs/CONTENT.md the house side is StructureDef data (a Shapes and StageCosts row plus a model), and tile making is a ClayForm plus a ProcessDef, with no new switch.

## Progress log
- 2026-10-16 22:30: Justin picked roof tiles via the PM. Ticket opened. Asking House building (session_01DD9p6EhSc5pJ8CjNmDFTrG) how tiles go onto roofs; options in Open questions.

- 2026-10-16 22:50: House building agreed option 1 (Tiled house kind); they add the Shapes row, costs and model (after their building-12 folds StageCosts into the shape); I give them Item.RoofTile (121). Built: ClayForm.Tile (kiln-only), items 120 Unfired Roof Tile / 121 Roof Tile, clay.tile at the bench (1 tempered clay -> 1 tile; 12 tiles on a roof is the same clay as 24 at 2 per clay; a kiln holds 6), curved-slab mesh and looks, campfire refusal names the piece, Journal line (clay.tile needs bench + kiln). ClayCheck --tile PASSED (c10tile2). Numbers sent to the PM as options (tile count, weathering hours); not answered yet. Kiln firing of tiles goes through the same path as the jar (--kiln --jar).

## Open questions / blockers
Sent to House building and the PM, 2026-10-16 22:30. How do tiles get on a roof? Working on option A until told otherwise.
1. (Recommended) A new house kind, "Tiled house": a brick house whose last stage is "Roof tiles" (tile cost) instead of thatch. Pure data, no world change; players choose thatch or tile when they lay out the house. Cost: two house kinds that share most of their shape.
2. A separate "Tile roof" structure laid over a finished brick house or clay hut as an upgrade: players with existing houses can re-roof. Cost: an overlay structure that needs a new rule (it sits on a home), more code.
3. Add "Roof tiles" as an optional extra stage on the existing brick house: no new kind. Cost: it changes the existing kind's stage count, which touches stored houses.
Numbers I'd propose (start low, grow slowly, D-075): 1 tempered clay shapes 2 tiles (flat slab, no minigame, at the Potter's bench), kiln-fired only; a tiled house takes 24 tiles on the roof stage in place of 8 fiber of thatch (now planned as 12 tiles, 1 clay each, the same clay); it weathers after 504 h untouched (brick house 168 h).

## Next step
When the lamp PR #309 merges, merge main into claude/clay-ceramics-10-kfr4kr, run ClayCheck --tile and tools/upgrade-check.sh origin/main, open the PR and send the PM the green sha. Then (a) tell House building the final tile count and weathering hours once the PM answers, (b) extend ClayCheck to fire tiles in the kiln run. Older text follows.
Wait for House building's answer on how tiles attach (option 1 unless told otherwise); meanwhile add ClayForm.Tile (append after Lamp), items for an unfired and a fired tile (ids from the Ceramics range, next free after 119), and clay.tile at the bench.
