# building-15: Tiled house (ceramics-10)
Lane: House building
Owner: House building thread (Sonnet)
Status: in_review
Branch: claude/house-building-8ueqz2-tiled
PR: (see the PM message)

## Goal
Justin picked clay roof tiles (ceramics-10). Ceramics makes the tiles (`Item.RoofTile` 121, fired in a kiln); this is how they go onto a house: a new kind, the Tiled house, which is the mud-brick house with a roof of 12 fired tiles instead of thatch (Ceramics' option 1, agreed). Same room, door and shelter; Justin's numbers: 12 tiles on the roof stage, and the same claim-only and weathering rules as the brick house (no special weathering).

## Progress log
- 2026-10-17: built as data (docs/CONTENT.md). `StructureKind.TiledHouse` appended after RepairTable. One `Shapes` entry with its own `Costs` (stone/stick footings, brick/daub walls, stick/cordage rafters, 12 x RoofTile). Two new data flags instead of kind numbers: `BrickWalls` (brick house and tiled house; the wall stage is daub work, replacing the `5 =>` special case in `StageProcess`) and `TileRoof` (the model draws tiles, not thatch). The model: `BrickHouse(kind)` builds both, with `HouseRoof(..., roof)` taking the roof material; a rough terracotta `Tile` material for now, to be restyled with the art direction (D-232), and tile pieces count as roof for the see-through cutaway.
  - Also fixed a building-14 slip: the brick house's site pegs read the (already scaled) shape size and were then scaled again by the view; they now use the design size.
  - BuildTest: new shared-number checks for the tiled house (brick-walled home with tile roof, same stages, roof takes 12 tiles, other stages cost what the brick house's do, daub walls and roofing process, 1.25x, doorway admits a step). Fence mode and tiled checks pass (24 s), upgrade-check vs origin/main passes. The full tiled build (bricks and tiles) is not run in BuildTest: it needs kiln-fired tiles.

## Open questions / blockers
- The tile roof reuses the thatch row mesh in terracotta: a rough prototype. Modeling should give it real tile rows (low-poly, painted, D-232) once Justin's look is set.
- Journal 'Things to try' line for the tile build: Ceramics said they add the tile process line; the house itself needs none new (its stages report the same build.* processes).

## Next step
Wait for CI, send the PM the green sha, ping Ceramics so ClayCheck can include it, then ask Modeling for the tile-row mesh.
