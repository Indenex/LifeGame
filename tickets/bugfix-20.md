# bugfix-20: shaping clay at the 24-piece cap refunds raw-quality clay and says "Your pot is drying"
Lane: Bug fixes
Owner: Bug fixes thread
Status: todo
Branch: claude/bugfix-20
PR: (none yet)

## Goal
With 24 of your clay pieces already drying, shaping a pot or brick gives back exactly the clay it took, at the quality it had. The only message is "You have too much clay drying already..."; there is no "Your pot is drying" note and no dry-by-fire tip.

Cause (found in the clay-path review, 2026-10-16): server/spacetimedb/Ceramics.cs OnPlaced refunds `def.Consumes` at `Items.RawQuality` (50), so tempered clay of quality 80 comes back at 50. CraftJob stores no input quality to refund with. client/scripts/ceramics/CeramicsView.cs MyCraftJob.OnDelete always says the piece is drying when the job did not fail.

Recommended fix: refuse the craft before it starts (StartCraft in Crafting.cs would need a small hook, so ask Crafting first), so nothing is consumed and nothing is refunded. Fallback: keep the refund, give it the job's Quality capped at the inputs' best quality in the bag, and have the client skip its drying note when a "too much clay" news line arrives for that job.

## Progress log
- 2026-10-16: filed from the review; low severity (needs 24 pieces drying at once).

## Open questions / blockers
- Which fix: ask the PM/Crafting whether a pre-start hook in StartCraft is acceptable.

## Next step
Ask the PM whether bugfix-20 is worth doing now, and Crafting whether a pre-start refuse hook in StartCraft is OK; then implement the chosen fix on claude/bugfix-20.
