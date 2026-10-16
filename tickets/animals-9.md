# animals-9: Clothing from wool cloth and leather
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: in_progress
Branch: claude/animals-clothing-5gkntn (stacked on claude/animals-wool-cloth-5gkntn / PR #90; merge main in after #90 merges)
PR: (to open after #90 merges)

## Goal
The first clothes: a Wool tunic (sewn from wool cloth) and Leather foot wraps (from leather). Clothing you carry is worn: it slows how fast the cold drains your warmth, so a dressed player lasts longer at night and in rain. Done when Justin can shear, spin, weave and sew a tunic and see his warmth fall more slowly.

## How it works (defaults, pending Justin via the PM: Q3 a, Q4 a, Q5 a)
- Items (animals range): WoolTunic 200, LeatherWraps 201, HasQuality. `shared/Clothing.cs` (Animals-owned) lists each wearable: slot (Body, Feet) and insulation (tunic 0.25, wraps 0.10).
- Processes, tier 2: `sew.wool_tunic` (3 Wool cloth + 1 Wool yarn, needs Cutting, 25 s); `sew.leather_wraps` (1 Leather + 1 Cordage, needs Cutting, 10 s).
- Worn = carried: the best-quality piece per slot counts, so there's no equip UI. Death drops it like everything else.
- Server: Survival.cs gets a one-line `ClothingWarmthHook(ctx, playerId, ref insulation)`, and the cold drain is multiplied by (1 - insulation). `server/spacetimedb/Clothing.cs` implements it from the player's inventory. When the inventory changes, the SurvivalTick rate check picks up the new rate within a tick.
- No schema change.

## Progress log
- 2026-10-16 17:30: questions Q3-Q5 and the hook asks sent to the PM; started.

## Open questions / blockers
- Q3 wear model, Q4 pieces and numbers, Q5 visible on the avatar (sent to the PM).
- Survival hook OK and Skills `sew` prefix mapping (sent to the PM).

## Next step
Write shared/Clothing.cs, items, processes, the Survival hook and server/spacetimedb/Clothing.cs; build; local test of the warmth rate with and without the tunic.
