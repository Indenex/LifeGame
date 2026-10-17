# anim-7: Tool work that feels good (chop, dig, spade)
Lane: Animation
Owner: Animation session (Opus), session_019roNRWQBPw4ozNLDhzseig
Status: in_progress
Branch: claude/anim-7 (built on claude/anim-6, #485)
PR: (open once #485 merges)

## Goal
PM 03:29, for Justin: tool work should feel good. Chop needs weight, a hit on contact and recoil. Dig should be a push, a step on the blade, then a lift and throw of dirt. Add a spade variant for Crafting's new spade. The held tool shows in hand during each job. Both the old figure and the 1.35 m chibi read well. Impacts land on the same frame as Atmosphere's chips and dirt and Audio's thunks.

## Progress log
- 2026-10-17 03:40:
  - Chop rebuilt. A 0.45 s lift, a beat held at the top, then a downswing that gathers speed (cubic) into the bite at cycle 0.7. On the bite the knees dip, the body jolts and the axe kicks back about 0.14 before settling.
  - Dig styles from the tool (WorkMotions.DigStyleOf):
    - hands: kneel and scoop
    - digging stick: stab and lever (as before)
    - spade: set the blade, step the right foot onto it and drive it in, lever back, lift and throw the dirt left, over 1.35 s
  - The spade is found by name (Enum.TryParse "Spade"), so it needs no change when Crafting adds the item.
  - Impacts: PlayerAnimator fires Impact at the bite of a craft loop (chop 0.85/s at 0.7; knap at the snap) and at an action's contact moment (WorkMotions.ActImpact / ActThrow). Avatar re-raises it as the static `Avatar.Impact(avatar, motion, toolPoint, thrown)` for Atmosphere (chips, dirt puffs) and Audio (thunk, crunch).
  - Held tools: chop shows the axe through HeldTools (anim-3); dig shows the digging tool from player_act.
  - Hip drops scale with LegLength for the chibi.
  - Client builds.

## Open questions / blockers
- The spade's item name/id from Crafting: is it "Spade"?
- Atmosphere and Audio to hook Avatar.Impact (asked via the PM and Audio directly).

## Next step
When #485 merges: merge main, open the PR, send the green sha to the PM; Render clips of chop and of each dig style.
