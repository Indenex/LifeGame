# animals-9: Wool tunic
Lane: Animals
Owner: Animals lane (Sonnet)
Status: in_review
Branch: claude/animals-tunic-5gkntn (from main after #90 and #149; replaces the old claude/animals-clothing-5gkntn, which held pre-#149 history)
PR: (see below)

## Goal
The first body clothing: a wool tunic sewn from wool cloth at a crafting table, worn in the Body slot of the equipment panel, slowing the cold's drain on warmth by 10% at full quality (D-100; scaled by quality from 70% to 100% of that). Done when Justin can shear, spin, weave and sew a tunic, put it on and see his warmth fall a little more slowly.

## How it works
- Item `WoolTunic` 200 (HasQuality). Wearable row in `shared/Clothing.cs` (Body, insulation 0.10). The worn_item table, Wear/TakeOff, the Survival hook and the death drop already shipped with #149.
- Process `sew.wool_tunic`: 3 Wool cloth + 1 Wool yarn, a Cutting tool, at the crafting table (Station = Table), Base(2, 2) seconds. It trains Weaver (the `sew` prefix, since skills-11).
- Modeling already made the tunic's worn, ground and icon models (Garments.cs / ItemModels.cs, id 200).

## Progress log
- 2026-10-16 22:15: started from main (after #90, #149); added the item, the clothing row and the recipe. Server and client build, upgrade check against main passed.

## Open questions / blockers
- Journal 'Things to try' line for sew.wool_tunic (D-105): "Cloth and yarn from the wool of your sheep can be sewn into something you wear against the cold." Sent to Journal and skills.

## Next step
Wait for CI on the PR; send the PM the green sha.
