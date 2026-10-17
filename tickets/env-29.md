# env-29: Stumps and young trees block
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-shore-stones-server (with env-28, PM: alongside the parity fix)
PR: #592

## Goal
Justin: "stumps don't have collision, they should." A felled tree's stump, and its sapling and young-tree regrowth sized to match, block movement like a standing trunk, by one rule in shared/Obstacles that the client's collision, the check bots and (once trunks are enforced there, bugfix-27 follow-up) the server's movement check share.

## Progress log
- 2026-10-17: shared/Trees: StumpHeight, StumpLeft(p), RegrowSize(p) (the stages TreeFall draws: stump rots 14-28% of Trees.RegrowSeconds, sapling from 28%). ICellGround.FelledProgress (0..1, or -1). Obstacles.Regrowing: the stump blocks (hoppable, StumpHeight) while more than half is left (to about 21%); the regrowing tree blocks once a third grown (about 32%), trunk scaled with it; Obstacles.TrunkFoot shared. BotGround.FelledProgress from ChoppedTree (Felled, LastChop). Client: TreeFall sets Terrain.FelledProgress; TerrainCollision uses Obstacles.Regrowing; TreeFall draws from Trees.StumpLeft/RegrowSize. Server, client and every tool build; SpawnCheck passes locally.
- Note: the server's PlayerStepBlocked does not enforce trunks or boulders yet ("bugfix-27 follow-up"); when it does, stumps come with it through Obstacles.

## Open questions / blockers
- (none)

## Next step
Wait for CI on #592; send the green sha to the PM.
