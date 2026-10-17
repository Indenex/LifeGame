# anim-28: animation code quality pass
Lane: Animation
Owner: Animation thread (session_019roNRWQBPw4ozNLDhzseig)
Status: done
Branch: claude/anim-28
PR: #756

## Goal
No behaviour changes; players see nothing new. Tidy today's animation code (anim-12 to anim-27) after a read-only review:
- **Dead code:** the spade is now looked up as `Item.Spade` instead of by name through reflection, and a duplicate Dig arm in `ActLoop` is removed.
- **Duplication:** `RestStageOf` is shared by `RestTarget` and `SettleRest`, and `Avatar.RestoreHands` replaces three copies of the hand restore.
- **Comments:** stale comments are corrected (crafts can stand now, hands hold things, the quick-action list, the impact comments split between `ActImpact` and `WorkImpact`, the `LegJoints` comment placement, the AvatarWork header, the PlayerRig header).

Left out on purpose:
- Removing PlayerRig's Node3D-pivot path: it touches more code for little gain.
- A `RiseFall` helper.
- Caching `SetFace`: it needs a reset on a look change to stay identical. Possible later if profiling shows avatars cost.

## Progress log
- 2026-10-17 12:05: edits applied; client builds.
- 2026-10-17 11:58: CI green at 5da407f3; merged (#756).

## Open questions / blockers
- (none)

## Next step
Done: merged in #756.
