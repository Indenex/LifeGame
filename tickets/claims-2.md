# claims-2: Owner take-down, soft borders, weathering check
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session cse_01CfATdbRNPF9XXuAmTui6jb)
Status: in_review
Branch: claude/claims-chat-pvp-vk2qak
PR: (opening)

## Goal
D-044 and D-046/D-048 support. A claim owner can take down any building in their claim, members' included, and the materials drop where it stood, which is inside the claim. Claim borders show as a soft line on the ground that fades in within 6 m of the edge. There is one owner plus members, with no co-owner rank. Claims supply Building's weathering check: nothing inside a standing claim weathers. Justin sees the border line when walking near an edge, and can clear a member's hut from his land.

## Progress log
- 2026-10-15 23:15: restarted the branch from main after #8 merged (613215a). Owner take-down uses a `CanClearStructure(ctx, s, ref allowed)` partial in Building.cs Dismantle, implemented in Claims.cs. The border fade is `Social.BorderFadeDistance` = 6 m. `InClaimHook` is declared and implemented in Claims.cs. SocialTest has 25 checks and all pass locally on a fresh database.

## Decisions / defaults
- D-044: as above. Members can't take down other people's buildings; only the builder or the claim owner can.
- InClaimHook is declared in Claims.cs until Building's Weathering.cs lands. Whichever PR lands second keeps exactly one declaration.
- D-048 lock rule: Building can call the public `Module.ClaimAt(ctx, x, z)` and `Module.HasClaimRights(ctx, claim, ctx.Sender)` to let claim members lock and unlock doors.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the claims-2 PR; when it's green, send the head sha to the PM to merge.
