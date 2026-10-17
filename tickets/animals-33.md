# animals-33: no "Take eggs" or "Take the leather" prompt where the server would refuse
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-next-5gkntn
PR: (not opened yet)

## Goal
Inside someone's claim, a non-member is no longer offered "Take eggs" (the server refuses with a friendly line today). The prompt shows only when the server would say yes. The server stays the check (D-182); no look change.

## Progress log
- 2026-10-17: picked as the next item (D-145), told the PM. Client side written: `Herd.ClaimGoodsOpenAt` delegate used by `OfferEggs`, wired in Main.cs to `social.ClaimGoodsOpenAt`. That method does not exist yet: asked Claims (session_01CAk6p7Z42GfVwi8QMfNMhZ) to add it to SocialLayer.cs: `public bool ClaimGoodsOpenAt(float x, float z) => ClaimAt(x, z) is not { } c || IsOwnerOrMember(c);`. Landed in #810 (23c3035); merged into this branch.
- 2026-10-17: the hide rack's "Take the leather" prompt gets the same treatment, with its own rule (server: the maker, or the owner or a member of the claim the rack hangs in): `HideRacks.OfferLeather` offers a rack only if `MadeBy` is me, or a standing claim covers it and `ClaimGoodsOpenAt` is true. `ClaimStandingAt` (already public in SocialLayer) is wired next to it in Main.cs. Client builds (`dotnet build client/LifeGame.csproj` after gen-bindings). The Godot client could not be run here, so the prompt itself was not seen on screen; the logic is two boolean tests over data the client already has. A banned member still sees the prompt (the server refuses), as Claims noted.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, send the green sha to the PM.
