# animals-33: no "Take eggs" prompt where the server would refuse
Lane: Animals
Owner: Animals thread
Status: blocked
Branch: claude/animals-next-5gkntn
PR: (not opened yet)

## Goal
Inside someone's claim, a non-member is no longer offered "Take eggs" (the server refuses with a friendly line today). The prompt shows only when the server would say yes. The server stays the check (D-182); no look change.

## Progress log
- 2026-10-17: picked as the next item (D-145), told the PM. Client side written: `Herd.ClaimGoodsOpenAt` delegate used by `OfferEggs`, wired in Main.cs to `social.ClaimGoodsOpenAt`. That method does not exist yet: asked Claims (session_01CAk6p7Z42GfVwi8QMfNMhZ) to add it to SocialLayer.cs: `public bool ClaimGoodsOpenAt(float x, float z) => ClaimAt(x, z) is not { } c || IsOwnerOrMember(c);`. This branch does not compile until it lands.

## Open questions / blockers
- Waiting on Claims for `SocialLayer.ClaimGoodsOpenAt` (or their go-ahead to add it myself).

## Next step
When the method is on main: merge main, build the client (`dotnet build` of the client project after `tools/gen-bindings.sh`), open the PR, send the green sha to the PM. Same prompt rule for the hide rack's "Take leather" if it has an offer (check `HideRacks`): the server allows its maker or the claim's members.
