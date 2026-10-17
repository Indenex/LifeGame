# claims-30: --screenshot-town preview of the Town panel
Lane: Claims
Owner: Claims thread
Status: in_review
Branch: claude/claims-nxkyjn
PR: #839

## Goal
UX asked (for ui-72) for a `--screenshot-town` flag so Render can take before/after stills of the Town panel without a server. Launching the client with `--screenshot-town` opens the Town panel on a pretend town, "Willow Run": five signers on the map (Mira the Mayor, Tam (you) the Treasurer, Oren the Herald, Lise, Bram), a free neighbour and another town's marker nearby, an open Marshal ballot, a few town log lines, and an invitation from Birch Hollow pinned up as a note.

## Progress log
- 2026-10-17: TownPanel.Preview.cs added; Read/Spots/Rebuild/BuildLog use the pretend town when previewing, and the real connection is not attached while previewing.
- 2026-10-17: CI on a1bf34c9 and b11eadd6: SocialTest claims fell during the town checks (30 s lifetime); the town section now sets a 600 s lifetime once the decay check is done.

## Open questions / blockers
- (none)

## Next step
Wait for CI on #839; tell UX and the PM the flag exists.
