# animals-27: D-182 server-authority audit of the animal reducers
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-audit-5gkntn
PR: (to open)

## Goal
Audit every animal reducer against D-182 (reach, cooldowns, ownership, line of sight, who decides what), fix the gaps that are plain bugs, and bring the ones that are design choices to the PM.

## Audit (reducers: StrikeAnimal, ShootAnimal, ShearSheep, ButcherCarcass, CollectEggs, TakeLeather; AnimalTick is scheduled)
- All take only ids; every number (damage, yield, chance, quality) is decided on the server. The private `AnimalMind` (health, fear, flee direction) never reaches clients; the public `Animal` row carries only position, state, growth, fleece and eggs.
- Reach: Strike (World.Reach + weapon + slack for a running target), Shear, Butcher, Eggs and TakeLeather call `RequireReach` against the server's stored position; Shoot is capped at 40 m and a 0.7 rad cone around the stored heading.
- Held item: Strike, Shear and Butcher need the right tool in the equipped slot (`EquippedTool`); the bow shot needs bow and arrow (BowShotHook).
- Cooldowns: Strike was paced per animal only (1 s); Shoot paced per player by draw time; Shear/Butcher/Eggs consume their target, so they cannot repeat.
- FOUND and FIXED (this PR): (1) arrows ignored line of sight: shots went through tree trunks and walls. `ArrowBlocked` now walks the line of fire in 1 m steps with the shared obstacle rule (trunks stop it, boulders and low fences under chest height do not) and the structure wall hook; a blocked shot throws before ammo is used. (2) A player could strike several animals within one second: `SwungAt` on `AnimalPlayerSeen` (private table, `[Default(0L)]`) paces a player's swings to one per 0.5 s on any animal.
- NOT changed, design choices for Justin (sent to the PM): (a) anyone can take eggs from any hen, including hens inside someone else's claim; (b) `TakeLeather` lets anyone take another player's hide rack leather (Leather.cs has `MadeBy` but no claim or owner check); (c) butchering and shearing do not check claim rights either.

## Progress log
- 2026-10-17: audit done; the two fixes built (server builds); upgrade check to follow in CI.

## Open questions / blockers
- (a) to (c) above: who may take what inside a claim. Options sent to the PM.

## Next step
Wait for CI, send the green sha to the PM.
