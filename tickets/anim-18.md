# anim-18: Carrying heavy things in both arms
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: done
Branch: claude/anim-18 (from main)
PR: #692

## Goal
Holding something heavy (a storage jar, a pot of stew, a big water pot: Carry.Weight of 4 load units or more) makes the figure hug it in both arms in front, chest leaning back against the weight, with a slow side-to-side waddle while walking. Lighter tools stay in the one-handed carry. Everyone sees it, because the held item comes from the public PlayerEquip table. Option (c) from the PM's list.

## Design
- `Avatar.CarryTick` sets `animator.CarryingHeavy` when the carried, equipped item weighs at least `HeavyHold` = 4 (`Carry.Weight`, data in shared/Carry.cs).
- `PlayerAnimator.Carry` blends in the two-armed hold and adds the waddle (hips sway, legs compensated) by walk amount.
- Also marks anim-13, 14, 15 and 17 done (all merged).

## Progress log
- 2026-10-17 10:00: built; client builds.

## Open questions / blockers
- (none)

## Next step
Wait for CI and send the PM the green sha. Ask Render C to include holding a storage jar in a later clip.
