# animals-14: Red deer
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: in_progress
Branch: claude/animals-deer-5gkntn (stacked on claude/animals-seasons-5gkntn / PR #195, which is stacked on Atmosphere's #173)
PR: #219 (draft until Q12)

## Goal
A second species (Q12 a, recommended; pending Justin via the PM): small herds of red deer at the woodland edges, far shyer than sheep. They notice a moving player from about 30 m and bolt into the trees; a hunter has to wait still at the edge or sneak in from downwind... (no wind yet: just slow and still). Hunting gives venison, a deer hide and an antler. Done when Justin sees a herd at a wood's edge in the evening, and can bring one down and butcher it.

## How it works
- Server `Animals.cs`: `AnimalSpecies.Deer` appended. The Sheep* constants became per-species `AnimalTraits` (Walk/Trot/Run, Notice, CalmSpeed, HomeRange, FleeAhead, WanderMin/Max, WoodPref, Curious, StrikeScale, Wary); sheep keep exactly their old numbers.
  - Deer: walk 1.1, trot 2.4, run 6.5 m/s; notice players from 30 m; closing faster than 1.2 m/s alarms; inside Wary = 10 m any movement unsettles them (a still player never does), so hunters wait at the edge for deer to graze near; bolt 12 m ahead; home range 18 m; leaders wander 4-12 m and prefer woodland 0.45 (the edge), sheep keep preferring open grass; never walk up to still players; a strike does 1.6x the damage (a spear kills in one strike).
  - Two herds (4 near (70,45), 3 near (-60,-75), settling at a wood's edge within 22 m). EnsureAnimals adds them to any world that has no deer yet (sheep-only worlds get them on the next connect; additive, no wipe).
- `Hunting.cs`: butchering a grown deer drops 2 Raw venison (202), 1 Deer hide (203) and 1 Antler (204); a fawn 1 venison. `hide.scrape_deer` scrapes a deer hide into the same Scraped hide (hand craft here; moves to the drying rack once #90 is on main).
- Fawns: the lamb logic (spring-only via #195, follow the mother) works for deer unchanged.
- Client `Sheep.cs`: the same skinned node and animation for both species; a deer mesh (slim barrel, deep chest, long neck and legs, big ears, red-brown coat in 3 shades, pale rump/belly/throat, dark nose and hooves) and a stag mesh with antlers (about a third of deer; stags 10% bigger).

## Progress log
- 2026-10-16 23:35: started (D-145) while Q12 is with Justin. Branch from the seasons branch to avoid conflicting with my own Animals.cs changes.

- 2026-10-17 00:20: PR #219 opened (draft). Render requested 987b355. Asked Crafting (cook.venison, antler use), Atmosphere/Audio (sheep-only bleats, deer calls), Modeling (polished deer + item models). Q13 (one-strike kill / venison count) sent to the PM.
- 2026-10-17 00:10: server + client written, build. Local bot test (scratch DeerCheck, TEMP give/teleport reducers, not committed): 2 herds (4 and 3) settle on dry ground at wood edges; walking at 4.2 m/s, sheep bolt at 7.0-7.9 m and deer at 17.6-20.6 m; sneaking at 1.5 m/s, sheep never bolt and deer bolt at 7.4-10.1 m; one spear strike brings a deer down; butchering gives 2 Raw venison, 1 Deer hide, 1 Antler. Upgrade check against the seasons branch passed, and the upgraded sheep-only world gained both deer herds.

## Open questions / blockers
- Q12 (which animal next) with the PM/Justin; built on (a) deer.
- Cooking venison and a use for antler: asked Crafting. Eating cooked venison: Survival's food table.
- Deer voices: Atmosphere.cs feeds every animal to the sheep bleats; asked Atmosphere/Audio to bleat only for sheep and give deer their own (an alarm bark when bolting).
- A polished deer model: asked Modeling (the procedural one here is the stand-in).

## Next step
Wait for Q12/Q13 and the lane replies; keep #219 current with #195 (merge it in when it changes).
