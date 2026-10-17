# bugfix-23: collision for trees, rocks and every structure
Lane: Bug fixes
Owner: Bug fixes thread
Status: done
Branch: claude/bugfix-23
PR: #228

## Goal
From Justin: "I want trees and houses etc. to have collision." Walking into a tree trunk or a rock slides you around it instead of through it. Every structure kind that should be solid is solid. Crowns stay walk-under. Client side only for now (BuildingSystem.Collide via Main.Walk); server movement checks are out of scope.

## Plan
1. Trees and rocks: the forest is deterministic per cell (Trees.Trunk, Trees.Forest, Hash01 salts in client/scripts/Terrain.cs AddVegetation). Add a cell-local lookup in Terrain (`ObstaclesNear(x, z)`, ask Environment, session_012hVejTxLoxBpZyX3GQmNL9, since they own Terrain.cs) returning trunk/rock circles for the 3x3 cells around a point, skipping cleared, dug, felled and water cells exactly like AddVegetation. Trunk radius is about 0.25 m times the tree scale, rocks use their footprint (about 0.45 m times their size).
2. Main.Walk: one-line hook after building.Collide pushes the step out of any circle (slide along the edge, no dead stop).
3. Audit every StructureKind for missing collision (lean-to frame, stations, hearths and fires, storage jars and baskets, kilns, log cabin on #202 / building-10) and add it in BuildingSystem.Collide.
4. Sheep and deer avoid tree trunks server side if cheap (Animals session_01SAcVBUUfPFFk1rdXuiQZRU, AnimalStepBlocked).
5. Render request: a clip of walking into trees, a rock and a house.

## Progress log
- 2026-10-16: ticket written from the PM's message.

- 2026-10-16: trees and rocks: client/scripts/environment/TerrainCollision.cs (Terrain.CollideVegetation) plus one line in Main.Walk; client builds locally (dotnet 8 at ~/dotnet, bindings generated). Not yet walked in a running client.

- 2026-10-16: audit: lean-to now blocks its sides and back (open front), claim marker made Solid; kilns, baskets, jars, huts, houses, fences already collided. Fires are not solid (you stand at them to use them); stations (#160) and hearth (#202) are not on main yet, so those lanes get a note. Client builds.

- 2026-10-16: #228 merged (trees, rocks, lean-to and claim marker collide); CI green on c397493 (https://github.com/Indenex/LifeGame/actions/runs/38962957529). Follow-ups: Environment extended TerrainCollision for giant trees and outcrops (#247); Animals will add server-side trunk avoidance; Crafting and House building set Solid on stations and the hearth.

## Open questions / blockers
- Need a lookup hook in Terrain.cs from Environment (asked).

## Next step
None; done and merged.