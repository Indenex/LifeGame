# ceramics-2: Updraft kiln
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session cse_017sscCMDfHcyeZNqGW22GBE)
Status: in_progress
Branch: claude/clay-ceramics-kfr4kr (restarted from main 8b96a02 after #11 merged)
PR: #21, #40 (both merged)

## Goal
A player builds an updraft kiln from sun-dried bricks and daub, loads dry pots into it, fuels and lights it, and gets back pots that rarely crack and come out a little better than pit-fired ones. Done when the kiln works end to end on a local server (ClayCheck extended), with screenshots of the kiln cold, firing, and unloaded.

## Design (D-035: blueprint build; the rest as proposed)
- Build: placed from Building's B menu as a blueprint, StructureKind.Kiln (index 3, after ClaimMarker). Stages: Base 4 bricks; Dome 4 bricks + 2 daub. Anyone can add materials. At most 2 finished kilns per player (my check). The interim crafted `clay.kiln` process is removed once the structure version lands.
- Hooks: Claims owns OnStructureComplete/OnStructureRemoved (one implementation per partial), so Kiln.cs finds the finished Structure lazily on first use and keeps firing state in its own row keyed by StructureId; one-line OnKilnRemoved call in Dismantle returns loaded pots. Waiting for #8 and #10 to merge; format checked with House building.
- Load: V beside a cold kiln moves up to 6 unfired pots from your bag into it.
- Fire: V beside a loaded kiln burns fuel from your bag (sticks or any fuel item, 180 s worth, so 2 sticks) and fires for 3 minutes (D-013).
- Result: each pot cracks 2% of the time (vs 4-24% in a pit) and comes out 10 quality points better, capped at 100. Pots drop beside the kiln; a news line tells whoever lit it.
- Server: own `Kiln` table + `KilnTimer` in `server/spacetimedb/Kiln.cs`; the load is stored on the kiln row, so `ClayPiece` doesn't change and no world wipe is needed.
- Client: brick beehive kiln with a chimney under `client/scripts/ceramics/`; it glows and smokes while firing, with a label showing load and time left.

## Progress log
- 2026-10-15 22:10: ticket created; branch restarted from main.
- 22:20: server Kiln.cs (load, fire, unload) and client KilnView (brick beehive, glow, smoke) pushed (ace271e); ClayCheck --kiln running. Justin chose blueprint placement (D-035); restructuring around Building's structures.

- 22:35: ClayCheck --kiln passed with the interim crafted kiln: 8 bricks dried by a fire, kiln built, empty kiln refused, 2 pots loaded, 2 sticks burned, 180 s firing, both whole at quality 80 (shaped at 70).
- 22:40: D-041 deep clay: ClayRules.DugClayQuality (80 at the stick-only depth); Crafting wires it into Dig. Kiln moved to the structure model (Kiln row keyed by StructureId, made on first use); crafted clay.kiln removed. Kind index is ClayRules.KilnStructureKind = 3 until StructureKind.Kiln exists.
- Prepared (local, on top of #8 and #10): StructureKind.Kiln + StageCosts (Base 4 bricks; Dome 4 bricks + 2 daub), Structures.Shapes entry (Radius 0.85, Solid, Warmth 0), StageProcess 3 => clay.kiln, StructureModel dispatch to KilnModel.Pieces(), CeramicsStructureRemoved partial, bot builds the blueprint.

- 2026-10-15 23:30: merged main 0c3964b (#20 digging stick, #18 subscriptions) at aa47c20. Kiln query now in net/Subscriptions.cs Area; GiveDugClay(ctx, player, depth) grades max(ClayRules.DugClayQuality(depth), Skills.DugClayQuality(rank)); kiln Use priority 26. Server, client, ClayCheck build.
- 2026-10-15 23:31: CI run https://github.com/Indenex/LifeGame/actions/runs/38823236524 green on 4716d4e; sha sent to PM for merge.
- 2026-10-15 23:40: #21 merged (85a1572). Claims is on main: TendClay, FirePots and UseKiln now call RequireClaimAccess; server builds. Rides with the blueprint PR. #10 still open.
- 2026-10-15 23:50: blueprint hooks re-applied on top of this branch + Building's branch (local prep f883e16): enum Kiln after ClaimMarker, StageCosts {Brick 4} / {Brick 4, Daub 2}, Shapes "Updraft kiln" (Round r0.85, Solid, Base/Dome), StageProcess KilnStructureKind => clay.kiln, StructureModel kind 3 => KilnModel.Pieces(), CeramicsStructureRemoved made the partial, casts replaced by StructureKind.Kiln. Builds; ClayCheck --kiln running.
- 2026-10-15 23:52: Building says building-3 (after #10) adds Basket as StructureKind 3, so Kiln becomes 4 (set ClayRules.KilnStructureKind = 4, append every Kiln row after Basket's). Agreed with Building.
- 2026-10-16 00:00: #40 merged (kiln blueprint as StructureKind 3, claim checks; also fixed main's CS0111). Kiln bot built the blueprint end to end locally. Remaining: in-game kiln screenshots for Justin.

## Open questions / blockers
- Sent to PM: the design defaults above.
- After Claims PR #8 is on main: RequireClaimAccess in TendClay, FirePots, OnPlaced and kiln use.

## Next step
Send the PM in-game kiln screenshots (blueprint, built, firing, done) via scratchpad kilnshots.sh-style run: bot builds and fires, a second player stands by the kiln and screenshots. Then mark this ticket done.