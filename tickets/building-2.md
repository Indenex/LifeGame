# building-2: Building on real items, skills and daub walls
Lane: House building
Owner: House building thread
Status: in_progress
Branch: claude/house-building-8ueqz2
PR: #10

## Goal
Building uses the shared `LifeGame.Shared.Item` ids. Each finished stage trains the builders' skills through Crafting's `OnProcessPerformed`/`QualityCapFor` hooks (build.frame, build.wattle, build.daub, build.roof) and gives the structure a quality. The clay hut's walls take Daub (110) from Ceramics. G is the shared Use key for adding materials (D-025). Claim owners can dismantle in their claim once Claims (#8) is on main. Lands after crafting-core (#4) and ceramics.

## Progress log
- 2026-10-15 21:50: branch restarted from claude/clay-ceramics-kfr4kr (which carries crafting-core) with the building files from main. Item ids, stage quality and skills hooks, Daub walls, G to add. BuildTest (two bots, lean-to and hut, daub crafted by a bot) passes locally in 150 s.

- 2026-10-15 22:00: merged claude/clay-ceramics-kfr4kr at ab0c72e (crafting-core e165799 with main, Survival included). Dropped BuildingSurvivalStub.cs, regenerated bindings, BuildTest passes in 147 s. BuildTest runs once in CI, as its own step.
- 2026-10-15 22:20: merged main 64812e0 (crafting #4). BuildTest skips clay cells already dug to the bottom (the load test can exhaust the nearest ones, which made Survival's CI flaky). LoadTest, CraftCheck and BuildTest pass back to back locally.
- Justin chose "As proposed" for the building-2 recipes (D-031): 12 daub hut walls, bricks later, lean-to keeps 3 fiber.

## Open questions / blockers
- (none)

## Next step
When ceramics #11 and claims #8 are on main: merge main, regenerate bindings, keep Claims' hooks in Building.cs, run BuildTest, then merge #10 when green (standing approval D-032) and send the PM the merge commit.
