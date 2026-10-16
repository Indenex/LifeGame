# animals-12: Leather foot wraps and worn clothing slots (D-128)
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: blocked
Branch: claude/animals-wraps-5gkntn (split out of animals-9 per D-128; stacked on claude/animals-wool-cloth-5gkntn / #90 per D-139)
PR: #149

## Goal
The first piece of clothing ships on its own: Leather foot wraps, made from leather and cordage with a blade, put on in the equipment panel (D-074, UX's ui-10). Worn, they take 4% off how fast the cold drains you, scaled by quality (D-100). This also lands the worn-slots server side that the wool tunic (animals-9, with the Spindle station and #90, D-120) will use. Done when Justin dries a hide into leather, makes wraps, puts them on in the panel, and his warmth falls a little more slowly.

## How it works
- Item LeatherWraps 201 (197-200 stay reserved for the wool items and the tunic in #90 / animals-9).
- Process `sew.leather_wraps`: 1 Leather + 1 Cordage, needs Cutting, 10 s, at the Leather drying rack station (D-139). `sew` trains Toolmaking (Skills mapped it, PR #43).
- `shared/Clothing.cs`: the contract with UX: `ClothingSlot` (Body 0, Feet 1), `Clothing.Find(item)` returns a Wearable with Slot, Insulation (wraps 0.04) and Effect line; insulation scales with quality from 70% (q0) to 100% (q100); `Clothing.Percent` gives "3% less cold"; cap 0.6.
- `server/spacetimedb/Clothing.cs`: public `worn_item` table (Id, Owner, PlayerId, Slot, Item, Quality) so others' clients can draw clothes; `my_worn` view; reducers `Wear(item, quality)` (server picks the slot, swaps the old piece back) and `TakeOff(slot)`. Worn pieces are out of the inventory and don't count toward carry (D-100).
- Survival.cs hooks (OK'd by the PM and the Survival lane): `ClothingWarmthHook`: cold drain x (1 - insulation), on the wetness cold line; `BeforeDeathDrop` at the top of Die, so worn clothes drop with everything else.
- Models: Modeling's Garments.cs/ItemModels.cs (claude/model-1-style). Panel: UX ui-10 (claude/ux-ui-equip-xlrh2s). On-figure: Animation.
- Schema: one new table.

## Progress log
- 2026-10-16 21:10: D-128 relayed: ship the wraps first. Split from animals-9 onto a branch from main. Local bot test (scratch WrapsCheck, TEMP give/die reducers, not committed): carried wraps change nothing; wearing q35 wraps gives warmth rate x0.9678 (matches Clothing, panel "3% less cold"); a stick is refused; taking them off restores the naked rate; on death the worn wraps drop to the ground. Server + client build; upgrade check passed.

- 2026-10-16 22:50: D-139: hold #149 and ship the wraps already at the rack. Merged claude/animals-wool-cloth-5gkntn (stations) in; sew.leather_wraps needs the DryingRack. PR base set to the wool branch (GitHub retargets to main when #90 merges); PR set to draft. Station bot test: wraps made at the rack and worn give warmth x0.9676. Upgrade check passed.

## Open questions / blockers
- Waits for #90 (wool + stations), which waits for Crafting's #160. UX's #151 (equipment panel) waits for this.

## Next step
When #90 merges, merge main in, rebuild, and mark #149 ready; then tell UX (session_01V6qgbNbvTwniqW946KACLr) #151 can follow.
