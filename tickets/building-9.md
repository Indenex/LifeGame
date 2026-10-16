# building-9: Raised hearth
Lane: House building
Owner: House building thread (session_01RrKHUxWRH973Ndnxsad9nv)
Status: in_review
Branch: claude/house-building-8ueqz2 (backup: claude/house-building-8ueqz2-hearth)
PR: #202 (opened 2026-10-16 19:54, head 5793230 plus ticket commits)

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

- 2026-10-16 19:50: #122 merged without FireTopUpHook, so the self-feed ships as a plain `HearthTopUp` method. Crafting put the hook up as #194; once it's on main, one line wires it up. Merged main into claude/house-building-8ueqz2 along with building-9. All local checks green: LoadTest, BuildTest (all modes in "both"), SpawnCheck, CraftCheck, SocialTest, and the upgrade check over origin/main.
- 2026-10-16 19:55: for Claims' claims-12 (claim fences), declared `GateOpenHook(ctx, s, ref bool allowed)`, called in ToggleDoor for gates, and the client `SnapFenceHook(target, ref snapped, ref yaw, ref done)`, called first in SnapFence. Claims implements both in its own files. BuildTest fence passes.

- 2026-10-16 19:54: opened #202. Render requested 5793230 (Render session_013qe8zPzzBHx9rFPEYoKMr5): hearth lit inside a hut at dusk, half-built, with a basket beside it.
- Claim fences (claims-12, Claims session_013ECn821EbTpbj4h7d3PepH), split the PM asked for:
  - House building keeps the Fence/Gate structures, models and collision.
  - Claims does the members-only gate rule (implementing `GateOpenHook` in its own server/spacetimedb/ClaimFences.cs) and border placement (`Social.SnapToClaimBorder`, plus implementing the client `SnapFenceHook` in its own `partial class BuildingSystem` file).
  - Lapsed claims are Claims' business. Server-side player walls are out of scope; the client already blocks players at fences, shut gates and walls.
  - Both hook declarations are in #202. Claims was told the sha.
- 2026-10-16 19:56: HANDOFF to a Sonnet session (D-159).

## Open questions / blockers
- Crafting's #194 (FireTopUpHook): once merged, add `static partial void FireTopUpHook(ReducerContext ctx, Fire fire, ref Timestamp fuelUntil) => HearthTopUp(ctx, fire, ref fuelUntil);` to BuildingHearth.cs.

## Next step
Watch CI on #202 (subscribe to the PR). When green, send the PM (session_01CvoSQGRe3vDvDXWQu2qruU) the head sha. When Crafting's #194 (FireTopUpHook) is on main, add the one-line partial from Open questions to BuildingHearth.cs, in #202 if it's still open or as a small follow-up PR. Then building-10 (tickets/building-10.md on origin/claude/house-building-8ueqz2-cabin).
