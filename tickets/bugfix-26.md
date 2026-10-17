# bugfix-26: CraftCheck "a fire sits on the boulder" (main Build 954 red)
Lane: Bug fixes
Owner: Bug fixes thread
Status: done
Branch: claude/bugfix-26
PR: (see below)

## Goal
main's CI goes green again: CraftCheck no longer fails with "a fire at (-63.1, 51.6) sits on the boulder at (-62.2, 52.0)", and the server and the check agree about when a boulder is gone.

## Progress log
- 2026-10-16: reproduced the pieces. The server and CraftCheck both use shared `Rocks` (same radius), and a probe on a live server shows `Rocks.LiesAt(-62,52)` true and `ClearForFire(-63.1, 51.6)` false on a fresh world, so FireSpot cannot have chosen that spot. CraftCheck line 133 checks every fire in the database, and in CI BuildTest runs first on the same database: its raised hearth sits inside a clay hut and the fire lit beside it goes on the hearth (BuiltHearthSpotHook, no ClearForFire). A building clears the ground under it (client BuildingSystem.SetCleared: cell inside a footprint plus 0.4 m), so the boulder is gone on screen and not collidable (bugfix-23). The check did not know that.
- 2026-10-16: fix: CraftCheck skips boulders under any structure footprint; the server's ClearForFire does the same (`UnderStructure`), so an invisible boulder under a hut no longer refuses a fire inside it. Server module and CraftCheck build. CraftCheck passed in CI order locally on main (fresh world, but its random spawn never went near the hut site, so the exact failing layout was not reproduced).

- 2026-10-16: #295 merged; CI green on d3dc140 (https://github.com/Indenex/LifeGame/actions/runs/38973470319); #307 (cheaper check order) merged, green on 73f395f.

## Open questions / blockers
- Placement of a building over a boulder is allowed (it clears it). Justin's fire ruling (only spots inside a boulder or trunk are refused) is unaffected.

## Next step
None; done and merged.