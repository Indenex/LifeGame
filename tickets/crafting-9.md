# crafting-9: Chop wood with the stone axe
Lane: Crafting
Owner: Crafting thread
Status: in_review
Branch: crafting-chop-wood
PR: #70

## Goal
The stone axe finally has a use. Standing next to a tree with an axe (anything tagged Chopping), G offers "Chop a log" (process `chop.log`, tier 2, 15 s). It gives a Log (item 15), which burns for 6 minutes on a fire, against 90 s for a stick. With `Trees.Felling` on, the third log fells the tree: it topples with the forest's existing fall animation, and it grows back 2 hours later. With it off, a tree gives one log of deadwood every 5 minutes and never falls. Felled trees sync to every player, and adding them needs no world wipe.

## Design
- `shared/Trees.cs` holds the tree placement rules, moved out of `client/scripts/Terrain.cs`, so the server knows where trees stand. Terrain uses the same functions, and the forest is unchanged.
- Server, `server/spacetimedb/Woodcutting.cs`:
  - Table `chopped_tree` stores only chopped trees: cell, logs taken, last chop, felled.
  - `StartCraft` picks the nearest standing tree within reach (`TreeToChop`).
  - The finished job adds the log and updates the row (`ChopTree`).
  - `RegrowTrees` runs every minute and clears rows older than the regrow or deadwood time. Its timer is created on the first chop, so worlds published before this change get it too.
  - Dug cells have no tree.
- Client, `client/scripts/crafting/Woodcutting.cs`:
  - Felled cells are added to `Terrain.Cleared`, so the tree topples, and when the row goes the tree grows back.
  - The `crafting.chop` Use handler offers the chop when you carry an axe and a standing tree is within reach.
- The switch is `Trees.Felling`; Justin chose felling on 2026-10-16 (D-060). Tunables: `LogsPerTree` 3, `RegrowSeconds` 2 h, `DeadwoodSeconds` 5 min.
- Known limit: a felled cell is drawn like a building clearing, so its one cell of grass flattens to earth around the stump spot.

## Progress log
- 2026-10-16 04:40: built; CraftCheck now fells the nearest tree with 3 logs and feeds a log to the fire. It passes locally on a fresh database.

- 2026-10-16 04:55: screenshots from the same spot, before and after felling: /mnt/project-files/screenshots/16-tree-standing.png and 17-tree-felled.png. The leafy tree left of centre is gone and leaves a bare patch. Deleting the row brings it back.
- 2026-10-16 17:00: D-060 noted (felling stays on, 3 logs). Integration merged main as 546afd3 (CI run https://github.com/Indenex/LifeGame/actions/runs/38935432796 was in progress); merged main again to pick up #87 (docs only).

## Open questions / blockers
- (none) Felling vs deadwood answered: D-060, trees fall after 3 logs.

## Next step
Wait for CI on PR #70's new head, then send the PM the green sha.
