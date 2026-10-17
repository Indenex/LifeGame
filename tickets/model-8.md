# model-8: Requested item models (waterskin, spade)
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/model-8-items
PR: (opening)

## Goal
Item models other lanes asked for, in ItemModels (ground, inventory icon, held): Survival's hide waterskin, empty (161) and full (162); Crafting's wooden spade with a stone edge (18). Keyed by number until the ids reach main.

## Progress log
- 2026-10-17 05:20: Spade (short handle, forearm-long flat blade, lashed pale stone edge; held like the digging stick), Waterskin empty (slack, folded flat, neck flopped and tied) and full (swollen, taut neck tied up). Showcase `items --ids=18,161,162`.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR; send the PM the green sha; switch the numbers to Item.Spade / Item.Waterskin / Item.WaterskinOfWater once those reach main.
