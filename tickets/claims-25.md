# claims-25: ClaimGoodsOpenAt for the Animals prompts
Lane: Claims
Owner: Claims thread
Status: in_review
Branch: claude/claims-25-goods-open
PR: (see PR list)

## Goal
Animals asked (PM overnight defaults: eggs and hide-rack leather for the claim's owner and members) for a client helper so its "Take eggs" prompt isn't offered to a non-member inside someone's claim.

## Progress log
- 2026-10-17: `SocialLayer.ClaimGoodsOpenAt(x, z)`: true when no standing claim covers the point, or the player owns it or is a member the owner added (townmates not counted, as the server's `IsOwnerOrMember`). Prompt-hiding only; the server still decides, and a ban is not mirrored on the client (a banned member sees the prompt and the server refuses). Client builds against fresh bindings.

## Open questions / blockers
- (none)

## Next step
Wait for CI, send the green sha to the PM; Animals wires it into Herd from Main.cs.
