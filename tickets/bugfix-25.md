# bugfix-25: jump (Space)
Lane: Bug fixes
Owner: Bug fixes thread
Status: todo
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

## Open questions / blockers
- The four choices above go to Justin through the PM.
- Claim fences from the Claims lane (claims-12) are ordinary Fence structures inside a claim; confirm with Claims that a "claim fence" means exactly that.

## Next step
Branch off main once #228 has merged; write shared/Jump.cs with a unit check in tools, then the client hooks.
