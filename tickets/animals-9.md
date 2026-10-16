# animals-9: Clothing from wool cloth and leather
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: in_progress
Branch: claude/animals-clothing-5gkntn (stacked on claude/animals-wool-cloth-5gkntn / PR #90; merge main in after #90 merges)
PR: (to open after #90 merges)

## Goal
The first clothes: a Wool tunic (sewn from wool cloth) and Leather foot wraps (from leather), put on in an equipment panel with Body and Feet slots (D-074). Worn clothing slows how fast the cold drains your warmth, modestly at first (D-075). Done when Justin can shear, spin, weave and sew a tunic, put it on in the panel and see his warmth fall a little more slowly.

## How it works
- Items (animals range): WoolTunic 200, LeatherWraps 201, HasQuality.
- `shared/Clothing.cs` (Animals-owned, the shared contract with UX): Item -> slot (Body 0, Feet 1), insulation at full quality, a plain effect line. Insulation scales with quality from 70% (q0) to 100% (q100). Defaults (proposed to the PM per D-075): tunic 0.08, wraps 0.03; cap 0.6.
- Processes, tier 2: `sew.wool_tunic` (3 Wool cloth + 1 Wool yarn, needs Cutting, 25 s); `sew.leather_wraps` (1 Leather + 1 Cordage, needs Cutting, 10 s). `sew` trains Toolmaking (Skills lane mapped it in PR #43, 7b28d4a).
- Server `server/spacetimedb/Clothing.cs`: public table `worn_item` (Id, Owner, PlayerId, Slot, Item, Quality) so every client can show what others wear; view `my_worn`. Reducers `Wear(item, quality)` (takes one from the inventory, server picks the slot, swaps the old piece back) and `TakeOff(slot)`. Worn pieces are out of the inventory, so they don't count toward the carry load.
- Survival.cs hooks (OK'd by the PM and Survival): `ClothingWarmthHook(ctx, playerId, ref insulation)`: cold drain x (1 - insulation), worn pieces only. `BeforeDeathDrop(ctx, player)` at the top of Die: worn clothes go back to the inventory so they drop with everything else.
- Client panel: UX/UI's ui-10 (branch claude/ux-ui-equip-xlrh2s) through IEquipLink. On-figure clothes: Modeling and Animation lanes (requested).
- Schema: one new table only.

## Progress log
- 2026-10-16 17:30: questions Q3-Q5 and the hook asks sent to the PM; started.
- 2026-10-16 19:10: D-074/D-075 relayed. Reworked to worn slots: worn_item table, my_worn, Wear/TakeOff, BeforeDeathDrop; numbers lowered to 0.08/0.03 with quality share. Local bot test: carried clothes change nothing; wearing the q35 tunic gives x0.9356 (matches), + wraps x0.9115, a stick is refused, taking the tunic off goes back to wraps only. Upgrade check passed.
- 2026-10-16 19:55: merged main cc75409 (#75 in): the insulation factor now sits on the wetness cold line `... * Chill(ctx, v.Wetness)`. Server + client build, ClothCheck passes (same ratios, death drop), upgrade check passed.
- 2026-10-16 19:45: Modeling built the tunic and foot wraps on claude/model-1-style eb8b399 (client/scripts/models/Garments.cs): worn, ground, icon and held models; Animation wires worn_item to PlayerModel.Wear. Nothing more needed from the server; dye later = a colour per row.
- 2026-10-16 19:30: Survival reviewed both hooks (OK; after #75 the factor goes on the line ending `* Chill(ctx, v.Wetness)`). UX merged 5cc840a into claude/ux-ui-equip-xlrh2s (1931044), subscribes my_worn in Subscriptions.cs, OK with worn not counting toward carry; wants the animals-9 PR number. Animation will skin garments to the player skeleton and read worn_item once Modeling's PlayerModel and this branch are on main.
- 2026-10-16 19:20: death drop tested (TEMP TestDie): both worn pieces cleared from worn_item and lie on the ground.
- 2026-10-16 18:35: merged animals-8's spin minigame branch in.
- 2026-10-16 18:00: written; server + client build. Local bot test (scratch ClothCheck with a TEMP give reducer, not committed): spin 13 yarn, weave 3 cloth, sew tunic q35 -> warmth rate -0.00104 to -0.00078 (x0.750); foot wraps -> -0.00068 (x0.650). Upgrade check against main passed. Carry limit note: 26 wool at once is over the carry limit, so players spin as they go or use a basket.

## Open questions / blockers
- Answered: D-074 paper-doll panel (UX builds it); D-075 low numbers that climb slowly. PM OK'd the Survival hooks; Survival agreed to ClothingWarmthHook (told about BeforeDeathDrop).
- Numbers proposed to the PM (tunic 8%, wraps 3%, quality 70-100%).
- Survival's #75 reworks the cold line: after it merges, merge main in and re-apply `* (1f - Math.Clamp(insulation, 0f, 1f))` on the new `cold` line only.

## Next step
After #90 merges, merge main in, open the animals-9 PR and send its number to UX (session_01V6qgbNbvTwniqW946KACLr).
