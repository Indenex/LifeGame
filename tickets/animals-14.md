# animals-14: Red deer
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: in_progress
Branch: claude/animals-deer-5gkntn (stacked on claude/animals-seasons-5gkntn / PR #195, which is stacked on Atmosphere's #173)
PR: (to open)

## Goal
A second species (Q12 a, recommended; pending Justin via the PM): small herds of red deer at the woodland edges, far shyer than sheep. They notice a moving player from about 30 m and bolt into the trees; a hunter has to wait still at the edge or sneak in from downwind... (no wind yet: just slow and still). Hunting gives venison, a deer hide and an antler. Done when Justin sees a herd at a wood's edge in the evening, and can bring one down and butcher it.

## Plan
- Server: `AnimalSpecies.Deer` (appended). Per-species traits (speeds, notice range, calm speed, home range, wood preference, curiosity, how hard a strike hits) instead of the Sheep* constants; sheep keep today's numbers exactly.
- Deer prefer the woodland edge (woodland ~0.45) instead of open grass; never walk up to still players; bolt far (12 m) and fast (6.5 m/s); a herd of 3-4 with a stag.
- Two herds placed on first need (EnsureAnimals also adds deer to worlds that only have sheep; additive, no wipe).
- Butchering a deer: items in the animals range (RawVenison 202, DeerHide 203, Antler 204). Cooking venison and scraping a deer hide: ask Crafting (cook) and reuse hide.* (scrape a deer hide into the same Scraped hide).
- Client: a procedural deer (slim, long-legged, brown with a pale rump; antlers on the stag), skinned like the sheep (animals-7); Modeling asked for a polished one.
- Seasons (D-135 via #195): fawns in spring like lambs.

## Progress log
- 2026-10-16 23:35: started (D-145) while Q12 is with Justin. Branch from the seasons branch to avoid conflicting with my own Animals.cs changes.

## Open questions / blockers
- Q12 (which animal next) with the PM/Justin; building (a) deer.

## Next step
Refactor Sheep* constants into per-species traits in Animals.cs, add AnimalSpecies.Deer and the deer herds; bot-test sheep unchanged and deer behaviour.
