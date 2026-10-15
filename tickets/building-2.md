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

## Open questions / blockers
- Bricks (109): the wattle-and-daub hut doesn't use them. Default: save bricks for a later mud-brick house. Sent to the PM.
- Lean-to lashing still uses 3 fiber (Justin approved those costs); cordage exists now and would be more realistic. Sent to the PM.

## Next step
PR #10 is ready once CI is green; it lands after Ceramics (it carries the Ceramics branch). Ask the PM for Justin's merge after Ceramics merges. Then: claim owners dismantle in their claim (Claims #8), G as a Use handler (crafting-2).
