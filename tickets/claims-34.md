# claims-34: "Entering <town>" note
Lane: Claims, chat and PvP rules
Owner: Claims thread
Status: in_progress
Branch: claude/claims-nxkyjn
PR: (not yet opened)

## Goal
Walking into a town's land shows a gentle "Entering Willow Run" (the town's own name, plus the claim's place name on a second line if it has one) that fades in and out; walking out shows a quieter "Leaving Willow Run". Each town speaks at most once a minute, so stepping over the edge and back does not repeat, and moving between claims of one town says nothing. Client only, in SocialLayer.Places.cs (the claims-9 named-places label); no server change. Justin liked the town message.

## Progress log
- 2026-10-17: built in `SocialLayer.ArriveAt` (town from public TownSigner and Town), 60 s cooldown per town, `--screenshot-entering` preview. Not built locally (no dotnet); CI is the build.

## Open questions / blockers
- (none)

## Next step
Push, open PR, wait for CI green, ask Render for a half-size `--screenshot-entering` shot, one message to the PM.
