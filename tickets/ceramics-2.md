# ceramics-2: Updraft kiln
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session cse_017sscCMDfHcyeZNqGW22GBE)
Status: in_progress
Branch: claude/clay-ceramics-kfr4kr (restarted from main 8b96a02 after #11 merged)
PR: (milestone A, opening now: kiln firing logic, kiln look, deep clay grade)

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

## Open questions / blockers
- Sent to PM: the design defaults above.
- After Claims PR #8 is on main: RequireClaimAccess in TendClay, FirePots, OnPlaced and kiln use.

## Next step
After #8 and #10 merge: merge main, add the prepared Building/Structures/StructureModel hooks and KilnModel.Pieces, make CeramicsStructureRemoved the partial, run ClayCheck --kiln, take in-game kiln screenshots.
