# bugfix-32: Walk through water
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-32
PR: (to open)

## Goal
Justin: "I can't walk through water in the ponds... I don't need a swimming animation yet, just able to walk, maybe float through if it's way too deep." Players can walk into all water.

## Design (defaults from the PM, D-075 low-start numbers)
- Shared `shared/Wading.cs`: `Depth`, `SpeedFactor`, `Floating`, `FeetY`.
- Wading: feet follow the bed; speed eases from 100% at the shore to 60% at 0.9 m depth.
- Floating: bed deeper than 0.9 m below the surface: feet clamp at WaterLevel - 0.9 (head and shoulders stay out), speed eases on to 50%, no stamina cost, no new animation, no jumping.
- Getting out always works: nothing blocks a step into or out of water any more.
- Main.cs: the old "deep water is not walkable" block is removed; three small calls to `Wading` in `Walk`.
- Server: no change needed. Nothing on the server rejects water positions, and the pace budget (`MoveAllowance`) is a maximum, so slower wading is always inside it. The server does not enforce the wading slow-down: an honest client and the server may differ slightly on bed depth at the shore, and a false rejection there would feel worse than the tiny speed gain.
- `shared/Land.cs` comment updated.
- SpawnCheck: a bot finds water with dry ground on both sides, walks across and comes out the other side (passes locally: 30 m of river).

## Progress log
- 2026-10-17: implemented, builds, SpawnCheck passes locally.

## Next step
Open PR, wait for CI, send the PM the green sha; tell Integration about the Main.cs hook.
