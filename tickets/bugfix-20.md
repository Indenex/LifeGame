# bugfix-20: shaping clay at the 24-piece cap refunds raw-quality clay and says "Your pot is drying"
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_progress
Branch: claude/bugfix-20
PR: (none yet)

## Goal
With 24 of your clay pieces already drying, shaping a pot or brick gives back exactly the clay it took, at the quality it had. The only message is "You have too much clay drying already..."; there is no "Your pot is drying" note and no dry-by-fire tip.

Cause (found in the clay-path review, 2026-10-16): server/spacetimedb/Ceramics.cs OnPlaced refunds `def.Consumes` at `Items.RawQuality` (50), so tempered clay of quality 80 comes back at 50. CraftJob stores no input quality to refund with. client/scripts/ceramics/CeramicsView.cs MyCraftJob.OnDelete always says the piece is drying when the job did not fail.

Recommended fix: refuse the craft before it starts (StartCraft in Crafting.cs would need a small hook, so ask Crafting first), so nothing is consumed and nothing is refunded. Fallback: keep the refund, give it the job's Quality capped at the inputs' best quality in the bag, and have the client skip its drying note when a "too much clay" news line arrives for that job.

## Progress log
- 2026-10-16: filed from the review; low severity (needs 24 pieces drying at once).
- 2026-10-16: started (PM queued it; D-145). Implemented fix (a): Crafting.cs gets `static partial void CanStartCraft(ctx, playerId, def, ref string? refusal)`, called in StartCraft after the input checks and before anything is taken; Ceramics implements it and refuses clay forms at 24 pieces out ("You have too much clay drying already. Pick some up or fire it first."). OnPlaced keeps its refund as a fallback. Server builds. Asked Crafting (session_01Y2sA7pDBFch57bWb1uKMTq) whether the 3-line hook in Crafting.cs is OK. A scratch bot (24 pots, then a 25th) is running.

## Open questions / blockers
- Crafting to confirm the CanStartCraft hook in Crafting.cs (asked 2026-10-16).

## Next step
Read the scratch bot result; if it passes, push and open the PR (note the Crafting.cs hook for Crafting).
