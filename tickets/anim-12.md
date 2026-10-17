# anim-12: Sitting and lying down
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: in_progress
Branch: claude/anim-12
PR: (not yet)

## Goal
Players can sit on the ground and lie down, and everyone sees it. The H key sits you down. Pressed again while sitting, it lies you down curled on your side, head forward, a hand under the cheek, breathing slowly. A third press stands you up, and so does walking off. Getting down kneels first, then sits, then lies; getting up runs that backwards (about 0.55 s per stage). On a seat (a log or bench, once something sets PlayerRest.Seat), the thighs level out. Short chibi legs dangle and swing if they don't reach the ground. The nameplate comes down with the head. Survival's rest spots (#575, mats and beds) can fill SpotId and Seat later without changing the table.

## Design
- Server `server/spacetimedb/Resting.cs`: public table `PlayerRest {PlayerId PK, Kind (1 sit, 2 lie), Seat, SpotId}`. Reducers `Rest(kind)` (refused while flying or deeper than half float depth) and `StandUp()`. `StopResting` is called from Move (any step over 2 cm) and from StanceDisconnected.
- Client:
  - `animation/RestPoses.cs`: `RestKind` and the Sit/Lie poses, all measured from the rig (PlayerRig gains Thigh, Shin, HipHeight, HipWidth, Height).
  - `PlayerAnimator.Rest`/`Seat`/`Asleep` drive a staged layer (kneel, sit, lie); `RestLowering` is used for the nameplate.
  - `Avatar.Rest(...)`, the `RestRequested` event and the H key ("rest" action, bound in Avatar until UX adds a Controls row).
  - `AvatarWork`: player_rest insert/update/delete, plus catch-up when an avatar first appears.
- FK check: `scratchpad/rest.py` (chibi and adult, sit at 0/0.3/0.45 m, lie). Nothing below the ground.
  - The chibi's lower shoulder rests on the ground by tilting the chest up by asin((shoulderX - hipX)/torso).
  - The lie is on the side because some chibis have a shell.

## Progress log
- 2026-10-17 07:30: poses, layer, server table and reducers, client wiring done; client and server build.

## Open questions / blockers
- Survival: should resting do anything for gameplay (faster recovery, sleeping to pass the night)? Default: purely a pose. Who owns the sleep-at-a-mat button (default: their mat dialog calls Rest(2) and sets SpotId)?
- UX: a Controls row for "rest" (default key H, "Sit or lie down").

## Next step
Push, open the PR, request a Render C clip (sit, lie, get up; chibi and adult).
