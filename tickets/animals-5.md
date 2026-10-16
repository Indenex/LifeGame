# animals-5: Sheep hide scraped and dried into leather
Lane: Animals
Owner: Animals thread (session session_01XS7M1yLy2fLaFdE3HWnisM)
Status: in_progress
Branch: developed locally as animals-5-local on top of animals-4; goes to claude/animals-sheep-5gkntn once #51 merges
PR: (after #51)

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
  - Hook: Crafting.cs's Placed outcome sends `hide.*` processes to `PlaceHideRack` (one line, before Ceramics' `OnPlaced`). Asked Crafting whether they prefer a different fan-out; this line is the stand-in until they answer.
- Client `client/scripts/animals/HideRacks.cs` (created by Herd.Bind, no Main.cs change): an A-frame of crossed sticks with a crossbar and a sheepskin-shaped hide draped over it, colour lerped from wet (pale pink) to leather (tan) by moisture, sheen fading as it dries. G offers "Take the leather" (Use id `animals.leather`, priority 14) only when dry. Subscriptions.cs adds `hide_rack`.
- Schema is additive (new table only). Upgrade check against main passed 2026-10-16.

## Progress log
- 2026-10-16 01:20: server + client written, builds. Local run: scrape gave Scraped Hide q35, stretch placed a rack (rate 0.0250/s with a TEMP 40 s dry time), TakeLeather gave Leather q35 + 2 sticks and removed the rack. TEMP reverted to 360 s.
- 2026-10-16 01:45: draped hide mesh; screenshots screenshots/animals-06-hide-racks.png (wet rack in front, dry one behind) and animals-06-hide-racks-wide.png.
- 2026-10-16 01:50: merged main (via #51's branch, e8a767e). Upgrade check passed.

## Open questions / blockers
- Crafting: fan-out for Placed outcomes (prefix line vs a second partial). Stand-in line in Crafting.cs.
- Skills: `hide.scrape` and `hide.dry` are not `animal.*`, so Husbandry (D-049) doesn't catch them. Which skill (a Leatherworking skill, or Husbandry) is the Skills lane's call; asked via the PM.
- PM: whether to fold this into #51 or open its own PR after #51 merges (default: own PR after #51).

## Next step
When #51 merges, fast-forward claude/animals-sheep-5gkntn to animals-5-local merged with main, push, and open the animals-5 PR.
