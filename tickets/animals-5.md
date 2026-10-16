# animals-5: Sheep hide scraped and dried into leather
Lane: Animals
Owner: Animals thread (session session_01XS7M1yLy2fLaFdE3HWnisM)
Status: in_review
Branch: claude/animals-sheep-5gkntn (restarted from main after #51 merged; developed locally as animals-5-local)
PR: #63

## Goal
A Sheep Hide from butchering becomes Leather the way clay dries: scrape it with a blade, stretch it on a small stick rack set down in the world, and leave it. It dries over a few minutes, slower in rain and faster by a fire, visibly changing from pale wet hide to tan leather. When it is dry, G takes it down as Leather. Done when Justin can butcher a sheep and end up with leather in a build.

## How it works
- Items (shared/Items.cs, animals range 190-219): ScrapedHide 195, Leather 196.
- Processes (shared/Processes.cs), Tier 2:
  - `hide.scrape`: Sheep Hide + a Cutting tool, 8 s, gives a Scraped Hide at the crafter's quality.
  - `hide.stretch`: Scraped Hide + 2 Sticks + 1 Cordage, 6 s, Outcome.Placed: a hide rack in front of the crafter.
- Server `server/spacetimedb/Leather.cs`:
  - Public table `hide_rack` (position, heading, quality, Moisture at MoistureAt plus DryRate, like clay_piece).
  - `PlaceHideRack`: at most 4 racks per player; past that the inputs go back to the bag with a news line.
  - Dry rate is the clay dry rate (`DryRateAt`: rain, fire warmth) scaled so fair weather takes `HideDrySeconds` = 360 s. `DryHides` (from AnimalTick) re-reads the weather every 15 s per rack.
  - Reducer `TakeLeather(rackId)`: within reach and dry; gives Leather at the rack's quality and 2 Sticks back (the cord is used up), removes the rack, reports `hide.dry`.
  - Hook: Crafting.cs declares `static partial void AnimalsOnPlaced` beside Ceramics' `OnPlaced` and calls both for Placed outcomes; Leather.cs implements it and ignores anything not `hide.*` (approved by Crafting as integration owner).
- Client `client/scripts/animals/HideRacks.cs` (created by Herd.Bind, no Main.cs change): an A-frame of crossed sticks with a crossbar and a sheepskin-shaped hide draped over it, colour lerped from wet (pale pink) to leather (tan) by moisture, sheen fading as it dries. G offers "Take the leather" (Use id `animals.leather`, priority 14) only when dry. Subscriptions.cs adds `hide_rack`.
- Schema is additive (new table only). Upgrade check against main passed 2026-10-16.

## Progress log
- 2026-10-16 01:20: server + client written, builds. Local run: scrape gave Scraped Hide q35, stretch placed a rack (rate 0.0250/s with a TEMP 40 s dry time), TakeLeather gave Leather q35 + 2 sticks and removed the rack. TEMP reverted to 360 s.
- 2026-10-16 01:45: draped hide mesh; screenshots screenshots/animals-06-hide-racks.png (wet rack in front, dry one behind) and animals-06-hide-racks-wide.png.
- 2026-10-16 01:50: merged main (via #51's branch, e8a767e). Upgrade check passed.
- 2026-10-16 01:50: switched to the AnimalsOnPlaced hook; local stretch still places a rack.

- 2026-10-16 03:45: #51 merged; branch restarted from main 896e9bd+ with animals-5 merged in; server + client build, upgrade check passed.
- 2026-10-16 04:10: PM fold-in: fleece read near-white at dusk/night. Fleece albedo lowered (0.87→0.74 cream, 0.80→0.68 tan) and wool gets the painted shader's relief (detail 3, bump 1.2, cavity 0.3). Screenshots screenshots/animals-07-dusk-before/after.png, animals-07-day-after.png.

## Open questions / blockers
- (none) PM answered: hide.* maps to Husbandry for now (Skills lane added it to skills-10); own PR after #51; used-up cordage is fine.

## Next step
Wait for CI on the animals-5 PR, then send the PM the green sha.
