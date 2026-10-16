# claims-9: Name your camp
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session session_013ECn821EbTpbj4h7d3PepH)
Status: in_review
Branch: claude/claims-chat-pvp-vk2qak
PR: (not yet)

## Goal
A claim owner can name their land ("Willow Hollow") from the claim panel (K at the marker). The name then shows in three places:
- the land chip: "Willow Hollow · Mira's land · peaceful";
- the claim panel's heading;
- a soft title near the top of the screen for a few seconds as you walk in.

An empty name clears it, and the name goes when the marker falls. Names are tidied (extra spaces removed) and cut at 28 characters. No world wipe: names live in a new `claim_name` table beside `claim`.

## Progress log
- 2026-10-16 19:20: D-140 confirmed. Added Journal's ask: a finished claim marker reports process `claim.mark` (Social.MarkProcess, via Structures.StageProcess kind 2). SocialTest and BuildTest pass locally.
- 2026-10-16 19:15: built on the default while the PM decides between the claims-9 options (1 name your camp [recommended], 2 whisper/shout, 3 stash log).
  - Server: `server/spacetimedb/ClaimNames.cs` (the ClaimName table and the NameClaim reducer, owner only); RemoveClaim clears the name.
  - Shared: `Social.CleanClaimName` and `ClaimNameMax`.
  - Client: a "Name this place" box in ClaimPanel, the name on the land chip, and the arrival title in `SocialLayer.Places.cs`.
  - SocialTest: 5 new checks.
  - Local build passes; all social checks pass.

## Open questions / blockers
- (resolved) D-140: Justin picked Name your camp, no wipe. It's done as a new table rather than a defaulted column; same effect, and existing tables don't change.

## Next step
Wait for CI on the claims-9 PR; when it's green, send the head sha to the PM.
