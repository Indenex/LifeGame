# anim-5: Strike, bow, dig, shear, butcher, feed the fire, flinch
Lane: Animation
Owner: Animation session (Opus), session_019roNRWQBPw4ozNLDhzseig
Status: in_progress
Branch: claude/anim-5
PR: (to open)

## Goal
More of Justin's "other activities should have similar animations" (anim-4, #382), on the same player_act system. Actions that had no motion now get one, for everyone:
- Striking a player or animal: a spear thrust, blade slash or punch, from the weapon.
- Shooting a bow: side-on, draw to the cheek, loose.
- Digging: crouch and drive the stick into the earth.
- Shearing and butchering: kneel at the animal, short blade strokes.
- Feeding a fire: an underhand toss.
- A struck player flinches back.

## Progress log
- 2026-10-17 01:00: started after #382 merged. Picked as the next most useful item and told the PM.
  - shared WorkMotion appends Strike, Shoot, Dig, Shear, Butcher, Feed, Flinch.
  - Server hooks (NoteAct, one line each): Combat.StrikePlayer (with the weapon used for the damage), Hunting.StrikeAnimal (spear or blade), Hunting.ButcherCarcass, Shearing.ShearSheep, Reducers.Dig (digging tool), Crafting.FeedFire (the fuel).
  - Shoot comes from the existing ArrowShot event and Flinch from StrikeEvent (VictimId), so neither needs a server change.
  - Client: WorkMotions.StrikeStyle from the weapon's tags; poses for each; strikes, shots, feeding and flinching are upper-body so they play over walking; the bow shows in the left hand.
  - Server, client and tools build.

## Open questions / blockers
- Claims agreed (02:03) to drop SocialLayer's squash tween on a struck player; removed in this PR (SocialLayer.Fighting.cs, with Claims' OK).

## Next step
Open the PR, get CI green and send the sha to the PM; ask Render for clips (strike styles, bow, dig, shear, butcher, feed, flinch).
