# bugfix-24: hardening audit (anti-cheat before outside players join)
Lane: Bug fixes
Owner: Bug fixes thread
Status: todo
Branch: claude/bugfix-24
PR: (none; this is an audit, no code yet)

## Goal
Justin will host the server himself. Before outside players join, close every place the client decides something the server does not check (rule now, harden later). This ticket is the audit list only. Each fix gets scheduled by the PM and ticketed on its own. Prefer putting the rule in `shared/` and calling it from both the client and the server (`Move` and the reducers), so the two cannot drift.

Sizes: S = under half a day, M = about a day, L = several days.

## Audit list (found by reading server/spacetimedb/*.cs on main c39211d; verify each before fixing)

| # | Where | Risk | Fix | Size |
|---|---|---|---|---|
| 1 | `Move` (Reducers.cs): speed budget | `allowed = MaxSpeed * dt + MoveSlack` adds 1.5 m of slack on every call. A client that calls `Move` many times a second gets 1.5 m per call, so there is no real speed cap (speedhack and teleport in small hops). | Keep a per-player distance budget that refills at MaxSpeed (times the carry factor) and caps at the slack; each call spends from it. Put the formula in shared/. | S |
| 2 | `Move`: collision | Walls, fences, gates, huts, houses, trees and rocks (#228) collide client-side only. A modified client walks through them, into locked houses and over fences, and past shut doors. | Move the collision into shared/ (the same circles, fence lines and wall rules, fed by the Structure and Terrain data) and have `ApplyMoves` reject or slide steps. Terrain vegetation needs the shared tree and rock rules (Environment). The cheap first version only blocks structures; trees and rocks matter less. | L |
| 3 | `Move`: terrain, water and height | No check for deep water, or for steps that are too steep. Clients decide where they can stand. | Shared `CanStand(x, z)` using World.Height and the water level; reject steps into deep water unless climbing out (same rule as Main.Walk). | S |
| 4 | Sprint and stamina | Stamina and run speed are client-decided. The server only has a flat MaxSpeed and `Ran` (an XP report, rate-limited). A client can run forever, and can claim runs it did not make for run-skill gains. | Make stamina server-authoritative: derive running from the movement the server sees (speed over walk speed), drain and refill stamina in `Move`/`ApplyMoves`, and cap speed to walk speed when empty. `Ran` then becomes derived, not reported. | M |
| 5 | Crafting minigame `score` (`StartCraft`) | The minigame result is reported by the client; only the range is checked ("Invalid score"). A hacked client always reports 100, so top-quality pots and tools, and skill gains. | Cap the score by what the skill and tool allow (QualityCapFor is already there); add a plausibility floor on duration (the craft cannot finish faster than the minigame). Real fix: server-side minigame verification, which is L. First pass: clamp score to a per-process maximum plus the time check. | S (clamp) / L (verify) |
| 6 | Combat and hunting reach | `StrikePlayer` and `StrikeAnimal` check distance only (no line of sight, no facing, no cooldown on animals?). Auto-aim target choice and line of sight are client-side: a client can always "see" through walls or trees and strike from the maximum reach. | Add a facing check (heading within about 90 degrees of the target) and a line-of-sight test against structures (shared wall rules from item 2); confirm every strike path has a cooldown. | M |
| 7 | Hidden values sent to clients | Public tables carry every player's position (`PlayerPos`), all `Animal` rows with positions, state and mind-like data, `PlayerHealth`, `Roast`, `ClayPiece`, `BasketItem` (item, quality), `HideRack`. A hacked client reads them all, outside its view (finding players, herds, other bases' contents). The inventory is already private (`MyInventory` view). | Use views or per-chunk filtered views for positions and animals (only rows in or near your chunks); move quality, durability and other hidden values out of public rows into private tables served by `My...` views; list each public table and decide. Pair with the existing client chunk subscriptions (Subscriptions.cs), which are advisory only. | L |
| 8 | Client subscriptions are not a security boundary | The chunk subscriptions in `client/scripts/net/Subscriptions.cs` just narrow what a friendly client asks for. Any client can subscribe to `SELECT * FROM player_pos`. | Same as item 7: the fix is views on the server. | (in 7) |
| 9 | Reducer rate limits | Only Dig, Chat and Strike have cooldowns. `PlaceStructure`, `Drop`, `PickUp`, `SetName`, `Eat`, trade reducers, `FeedFire`, `StartCraft` and `SendChat` spam can flood the database (World items, structures, rows). One account can also create many characters if the identity is free. | A generic per-identity token bucket (calls per second per reducer, one helper in Reducers.cs) applied at `RequirePlayer`; per-reducer overrides for the cheap ones; a cap on unfinished building sites (exists: one) and on WorldItems per player. | M |
| 10 | Reach checks without line of sight | `Dig`, `PickUp`, `Forage`, `Build`, `BasketTake` check distance only, so items can be reached through a wall of a locked house (if the wall is a client-side collider only) or from the other side of a fence. | Needs item 2's shared wall rules, then one `RequireReachClear(from, to)` helper in place of `RequireReach`. | M (after 2) |
| 11 | Claims and access | `RequireClaimAccess` exists for Dig and PickUp. Audit the rest (basket and stash reducers, door toggles, `Dismantle`, `ToggleLock`) for the same check and for owner or member rules. | A table of reducer to access rule, with a test per row in tools/SocialTest. | M |
| 12 | Admin reducers | `MakeAdmin`, `SetTimeOfDay`, `SetSeason`, `SetClaimLifetime`, `SetNewcomerGrace` and similar. CI relies on some being callable. | Confirm each checks admin identity (not just any player), and that CI-only ones are guarded by a server setting that is off on a hosted server. | S |
| 13 | Time and drift | `LastMoveAt` and `Timestamp` come from the server, which is good; `Move` caps dt at 1 s. Check that a long gap between calls cannot bank a 1 s allowance repeatedly. Covered by item 1's budget. | (in 1) | - |
| 14 | Server-side logging of anomalies | Nothing records suspicious play, so cheating is invisible to the host. | A small private `Suspicion` table written by the checks above (speed over budget, rejected steps, rate limit hits) and a `suspicion_report` view for admins. | S |

## Suggested order
1. Item 1 (speed budget) and 3 (terrain): small, big effect.
2. Item 9 (rate limits) and 12 (admin).
3. Item 5 first pass (clamp) and 14 (logging).
4. Item 4 (stamina), then item 2 (shared collision), then 6 and 10 on top of it.
5. Item 7 (views for hidden values) last, since it changes what the client subscribes to and touches every lane's tables.

## Progress log
- 2026-10-16: audit written from the PM's request; no code.

## Open questions / blockers
- Items 7 and 2 touch other lanes' files; the PM should tell those lanes when they are scheduled.
- Does Justin want a server setting to switch the hardening on, so local test servers and CI keep working? Recommended: on by default, off with a `--dev` flag.

## Next step
The PM picks which items to schedule; take item 1 first (shared budget formula in shared/World.cs, used by Move).
