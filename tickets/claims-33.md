# claims-33: Town signboard on the main marker
Lane: Claims, chat and PvP rules
Owner: Claims thread
Status: in_progress
Branch: claude/claims-nxkyjn
PR: (not yet opened)

## Goal
A town's main marker (the standing claim on the roster longest, same rule as the store) carries a small wooden signboard hung from its post with the town's name, readable from both sides, so a town reads as a place from afar. Other markers stay plain. Client only, built from the public Town and TownSigner tables; no server change. The board follows the post when it leans as the marker weathers.

## Progress log
- 2026-10-17: built `ClaimMarker.SetSign`, `RefreshSigns` in SocialLayer.Town.cs (refreshes on Town, TownSigner and Claim changes), and a `--screenshot-sign` preview (pretend marker 5 m ahead of the player, "Willow Run"). Not built locally (no dotnet here); CI is the build.

## Open questions / blockers
- Style is Justin's pick (board size, wood colour, lettering): shot to the PM before merge.

## Next step
Push, wait for CI green, ask Render for a half-size `--screenshot-sign` shot, send the PM one message with the sha and the shot.
