# crafting-26: A spade (dig faster)
Lane: Crafting
Owner: Crafting thread
Status: in_progress
Branch: crafting-shovel
PR: (open after the local CraftCheck passes)

## Goal
Justin: "interact with the world more. Chop trees, dig with a shovel, etc." Add a spade: `Item.Spade` (18), carved from a log with a stone edge and cordage (`carve.spade`, Tier 2, needs a cutting tool), a Digging tool that wears and mends with cordage. New data field `ItemDef.DigSpeed` (spade 1.25, everything else 1): Dig divides its cooldown by the best working digging tool's speed (stick and hands unchanged), still server-validated (reach, claim, cooldown, depth +1 for any digging tool).

## Progress log
- 2026-10-17: built; server, client and CraftCheck compile. CraftCheck gained a spade section (a second dig at 0.85 of the stick cooldown must land). Asked Modeling for a model, Journal for a hint.

## Open questions / blockers
- CraftCheck result pending; open the PR and send the PM the sha.
- Chop loop review sent to the PM (no stump, no sapling stage, no partial-chop sign, spent axe still offered).

## Next step
Check /tmp/cc_spade.log (or rerun CraftCheck), push, open the PR.
