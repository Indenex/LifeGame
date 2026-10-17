# anim-11: The equipped tool in hand (carry, draw and stow)
Lane: Animation
Owner: Animation session (Opus), session_019roNRWQBPw4ozNLDhzseig
Status: done
Branch: claude/anim-11 (on UX's claude/ux-ui-equipslot-xlrh2s, #583, which is on Crafting's crafting-equip, #582)
PR: (opening)

## Goal
PM 05:54, for Justin: tools must be equipped to chop, dig, shear, butcher or fight with spear and bow (crafting-29). The equipped tool shows in the character's right hand with an idle and walk carry pose. Tool-use motions use the held model. Swapping plays a quick draw or stow (about 0.3 s). Other players see it too, from the public PlayerEquip row.

## Progress log
- 2026-10-17 06:00:
  - Avatar.Equip(item): the right hand holds, in priority order, a quick action's item, then the job's tool, then the equipped tool.
  - A change plays PlayerAnimator.Swap(), 0.3 s down to the hip and back, and the model swaps at the midpoint.
  - PlayerAnimator.Carrying: the forearm comes up a little to hold the tool clear of the leg, and the arm's walk swing is damped to 35%.
  - AvatarWork follows your own PlayerEquip and MyHotbar into me.Equip(Equipped.Item(conn)).
  - Client builds.

- 2026-10-17 06:10: Crafting added PlayerEquip.Item (kept in sync by SyncEquip) and the all-rows subscription on #582 (663a440). Merged crafting-equip in. AvatarWork now drives every avatar from PlayerEquip insert, update and delete (Owner to Player to avatar). An avatar coming into view takes its held item instantly, without a draw. Any item can be held (Justin: every slot equips); items without a held model hang from the top. Client builds.

- 2026-10-17 07:30: merged to main; marked done on anim-12.

## Open questions / blockers
- (none)

## Next step
None: merged. Follow-ups (clip fixes) go on later anim tickets.
