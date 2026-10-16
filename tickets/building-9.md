# building-9: Raised hearth
Lane: House building
Owner: House building thread (session_01RrKHUxWRH973Ndnxsad9nv)
Status: in_progress
Branch: local building-9, stacked on claude/house-building-8ueqz2 (#153); moves to that branch once #153 merges
PR: (not yet)

## Goal
D-087, second of three (fence, hearth, cabin). Justin picked option (a) (D-132). A raised hearth is built inside a finished clay hut or mud-brick house: a ring of stones (4 stone), then a clay surround (3 daub). A fire lit within reach of it goes onto it and burns 1.5× as long per stick, and it makes a hut fully warm, as a fire already does in the house. Smoke rises through the roof. Justin sees a stone-and-clay hearth by the wall with the fire on it.

## Progress log
- 2026-10-16 18:45: ticket opened. Design question sent to the PM: (a) recommended; (b) looks only; (c) also cooks faster.

- Crafting's Hearths.cs `FireSpot` places a new fire, rekindling a cold hearth within reach. `FeedFire` adds `FuelSeconds`. A built hearth needs two partial hooks declared by Crafting: one in `FireSpot` (a built hearth within reach takes the fire) and one in `FeedFire` and `LightFire` (fuel ×1.5 on a hearth). Building implements both with a `HearthAt(ctx, x, z)`. Ask Crafting once the design is confirmed.

- 2026-10-16 19:05: D-132, Justin picked (a). Rain doesn't put out fires today, so that part needs no code.
  - Shared: Hearth shape (Indoor flag, radius 0.45), costs 4 stone, then 3 daub; `HearthFuelScale` 1.5; `IsIndoor`/`IsHome`; hearth vs home clearance skipped.
  - Server: `BuildingHearth.cs` implements `BuiltHearthSpotHook` and `FireFuelScaleHook`; `HomeAround` requires 0.6 m inside a finished hut or house; `HearthFireIn` makes a hut fully warm.
  - Client: `StructureModel.Hearth.cs` (stone ring, clay surround and bowl) and a placement hint.
  - BuildTest clay-hut adds a hearth: refused outside, built inside by both, a fire lit beside it sits on it with 1350 s of fuel (900 × 1.5). It passed locally with Crafting's hooks patched in uncommitted. The patch is in the scratchpad as crafting-hearth-hooks.patch and was sent to Crafting.

- 2026-10-16 19:15: Crafting pushed the hooks in #122 (crafting-fire-cap). On a scratch merge of building-9 with #122, BuildTest clay-hut passes: the fire sits on the hearth with 1800 s of fuel (#122's StartSeconds × 1.5).

- 2026-10-16 19:36: Justin decided D-149 (principle D-148): a fire on a hearth pulls sticks and logs from nearby storage by itself and never needs tending. Proposed `FireTopUpHook(ctx, Fire fire, ref Timestamp fuelUntil)` to Crafting, called in BurnOutFires. Building's side: when a fire on a finished hearth has under 60 s left, take one log, else one stick, from a basket or jar within 3 m, and add FuelSeconds × 1.5 up to the fuel cap.

- 2026-10-16 19:38: FireTopUpHook implemented (BuildingHearth.cs). Scratch test merged with #122, with the proposed BurnOutFires call patched in and a throwaway reducer setting up a hearth, a basket (3 sticks, 1 log) and a fire with 25 s left. On the next tick it took the log and fuel went +540 s (360 × 1.5); sticks untouched. The cap uses the lighter's Firecraft rank (Fires.MaxSecondsFor × 1.5), so nothing more is needed from Crafting.

## Open questions / blockers
- Crafting: declare FireTopUpHook and call it in BurnOutFires (asked 19:36; the cap is handled on my side).
- Crafting's #122 declares and calls both hooks; tested. Building-9 waits for #122 and #153 to merge. Hooks: `BuiltHearthSpotHook` (FireSpot) and `FireFuelScaleHook` (LightFire, FeedFire, including the MaxSeconds cap). Asked 2026-10-16 18:50. This branch won't compile until those declarations are on main.

## Next step
When Crafting's hook declarations reach main and #153 has merged: merge main into claude/house-building-8ueqz2 along with this branch, run all checks, open the building-9 PR and request a render.
