# bugfix-25: jump (Space)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-25
PR: (none yet)

## Goal
Justin's request: a jump. Space jumps. A jump clears logs, rocks, banks and ordinary fences and gates. Justin's rule: "Fences too, but not claim fences unless you're a member of it." So a fence or gate that belongs to a claim blocks non-members even mid-jump; members can hop their own. Walls, huts and houses always block. Trees (trunks) block. A small stamina cost per jump. The arc and clearance rules live in `shared/` so the server can enforce them later (D-182).

## Plan (default numbers; all of them start low, D-075)
1. `shared/Jump.cs` (pure, no engine types):
   - Arc: launch speed and gravity give an apex of 1.1 m and an air time of about 0.65 s (`Jump.Height(t)`), planar speed fixed at takeoff (no air steering, only a little).
   - Obstacle heights: fallen log 0.5, rock = its radius x 1.6 (so up to about 0.6; outcrops of 1.0 m or more block), bank/step up to 0.9, fence 0.9, gate 1.0, pebbles 0.2. `Jump.Clears(obstacleHeight, airHeight)` is true when the arc is at least obstacleHeight + 0.1 at the crossing.
   - Grounded-only start, cooldown 0.4 s, stamina cost 8% of the bar, not allowed when winded, heavy loads lower the apex (Carry.WalkFactor).
   - `Jump.FenceBlocks(isClaimFence, isMember)`: a claim fence blocks non-members, no matter the height.
2. Client: `Main.Walk` gets a vertical state: while airborne the player's Y is terrain height + `Jump.Height(t)`, and `building.Collide` and `terrain.CollideVegetation` take the current air height so they skip obstacles it clears (one-line hooks in Main.cs, Integration's file).
3. Claim fences: a fence belongs to a claim when it stands within `Social.ClaimRadius` of a standing claim (`Social.InClaim`); membership comes from Claims' existing client data (SocialLayer claim and member rows). A pure helper in shared/ takes the claim list so the server can use it later.
4. Stamina: Survival owns it; the cost goes through their stamina step (a `Stamina.JumpCost` constant plus a `TrySpend` call). Ask Survival (session_01RedPo6NfcLBPNHQNVhC4qf).
5. UX wires Space in ui-16 (session_01V6qgbNbvTwniqW946KACLr): I expose `Jump.TryStart(...)`; they bind the key in the input map and KEYMAP.md.
6. Animation (session_019roNRWQBPw4ozNLDhzseig): jump and land poses. I expose `Avatar` state (airborne, rising or falling, landing impact) and they draw it. Audio (session_01SXcxtoyeqDmypiHxkHedU6): jump and landing sounds on the same events.
7. Server later (not in this ticket): Move checks the arc using the same shared rules (bugfix-24 item 2).

## Choices for Justin (recommended first)
- Apex: 1.1 m, clears fences (recommended) / 0.7 m, clears logs and rocks only, fences need a running start / 1.5 m, clears everything below a wall.
- Stamina cost: 8% of the bar (recommended) / 15% / free.
- Air control: none, keep takeoff direction (recommended) / slight steering.
- Jump while sneaking: allowed, lower apex (recommended) / not allowed.

## Progress log
- 2026-10-16: ticket and plan written from the PM's message; no code yet. Waiting for #228 (collision) and #221 to land, since this builds on #228's `CollideVegetation`.

- 2026-10-16: Claims confirmed: a claim fence is any Fence or Gate inside a standing claim's radius (Social.InClaim); no flag or table. Client: `SocialLayer.HasRightsAt(x, z)` (owner or member of the standing claim there) is public and already passed to Building as `building.MayBar`. Careful: it returns false both with no claim and with a claim you don't belong to, so a fence is a claim fence only if a standing claim covers it (the private claim lookup near SocialLayer.cs line 361; ask Claims to make it public or add `ClaimStandingAt(x, z)`). Server: `ClaimAt(ctx, x, z)` plus `HasClaimRights(ctx, claim, ctx.Sender)`; lapsed claims are ignored, so after a lapse fences count as ordinary.

- 2026-10-16: Justin's picks (via PM): apex 1.1 m (clears fences); 8% of the stamina bar per jump; no air control (keep takeoff direction); NO jumping while sneaking (so the sneak-lower-apex default is dropped). Still open: whether other players see your jump (asked PM; v1 shows your own only).
- 2026-10-16: lane hooks ready: Audio `Sfx.Play(Cue.Jump)` at takeoff and `Sfx.Land(strength, atmosphere.GroundAt(feet))` at landing (PR #242); Survival `SurvivalClient.TrySpendStamina(float fraction)` (PR #261); Animation `me.SetJump(airborne, rising, landing)` each frame from Main.Walk (branch claude/anim-2-jump). UX binds the `jump` action on Space (ui-16). Build after #228 merges, then add each hook as its PR reaches main.

- 2026-10-16: Justin picked (b): others see your jump. Design: a new `server/spacetimedb/Jump.cs` with a public table `PlayerJump { PlayerId (PK), Seq, At }` and a reducer `Jump()` the client calls on takeoff. The server refuses while sneaking (PlayerSneaking), under `Jump.CooldownSeconds`, and later when stamina or the arc don't fit (bugfix-24). The client plays your own jump at once (prediction); other players' clients react to OnInsert/OnUpdate on the row: Animation's `SetJump` on that avatar, `Sfx.Play(Cue.Jump)` positioned at it, and a vertical offset from `Jump.Height(now - At)`. The landing for a remote avatar is played when the arc ends (`Jump.AirTime`), strength 0 (a hop). Stamina stays client side for now (Survival's TrySpendStamina); the server check comes with bugfix-24.

- 2026-10-16: more hooks. UX: `jump` action on Space in PR #264 (ui-17, claude/ux-ui-keys-xlrh2s, 8959e3f); read `Input.IsActionJustPressed("jump")`, never Key.Space (rebindable). When the jump lands, change its row in client/scripts/ui/Controls.cs from `null` to `"Jump"` (F1 help list) and optionally `Hints.Request("jump")`. Merge after #264. Animation: remote avatars are the same Avatar class (Remote = true), so `SetJump` works on them; but a remote node glides to its server Target each frame, so for remote jumps do NOT set Position.Y: call `avatar.SetAirHeight(metres)` (added in PR #262) and set it to 0 on landing. Own avatar: move Y directly.

- 2026-10-16: Audio (PR #242): own jump `Sfx.Play(Cue.Jump)` and `Sfx.Land(strength, ground)`; other players' jumps `Sfx.PlayAt(Cue.Jump, worldPosition)` at takeoff and `Sfx.LandAt(0.3f, atmosphere.GroundAt(landingPos), landingPos)` at the end of the arc. They are quiet, fade out by about 30 m and pan, so no distance skip on my side.

- 2026-10-16: built. shared/Jumping.cs (arc: apex 1.1 m, 0.65 s; fence 0.8, gate 0.9, rocks 1.5 x radius; `FenceStops` makes a fence in a claim stop non-members even mid-jump), Stamina.JumpCost 0.08; server/spacetimedb/Jump.cs (public `player_jump` row + `Jump()` reducer: refuses while sneaking and under the cooldown); client: JumpMotion (own jump: frozen takeoff direction, TrySpendStamina, Sfx.Play/Land, me.SetJump), JumpView (others: SetAirHeight/SetJump from the row, Sfx.PlayAt/LandAt), Main.Walk hooks, Building.Collide(.., airHeight) and Terrain.CollideVegetation(.., airHeight) skip what the arc clears, `building.ClaimStandingAt = social.ClaimStandingAt` (new one-line public helper in SocialLayer), Controls.cs row gets Help "Jump", Subscriptions adds player_jump. The fence collision now steps out the side you were heading to when you land on top of a line. Verified: client and server build; a scratch bot against a local server: first jump writes seq 1, an immediate repeat is "Too soon", a jump while sneaking is refused ("You can't jump while sneaking"), a later jump is seq 3. NOT verified: the feel in the running game (no Godot display here); Render clip requested.

## Open questions / blockers
- The four choices above go to Justin through the PM.
- (answered, see log) claim fence = ordinary Fence/Gate inside a standing claim.

## Next step
Wait for CI on the PR, then send the PM the green sha; Render clip of jumping a fence, a rock, a claim fence as a non-member.
