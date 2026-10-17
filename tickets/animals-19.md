# animals-19: Hens and eggs
Lane: Animals
Owner: Animals thread
Status: in_progress
Branch: claude/animals-hen-5gkntn
PR: (to open)

## Goal
Justin picked the hen as the next animal. A small tame yard flock (an `AnimalDef` entry, id 2) near the clearing where players wake up. Each grown hen lays an egg every 1.5 game days and holds up to 3 until a player takes them, with no feeding or tending (D-148). Players "Take eggs" (no tool, reach) for the Husbandry skill. Hens can be hunted/butchered like other animals for raw chicken and a feather; chicken roasts like venison. Chicks follow their mother as lambs do (shared code).

## Progress log
- 2026-10-17: Def, items (Egg 207, RawChicken 208, CookedChicken 209, BurntChicken 210, Feather 211), foods, `cook.chicken`, `animal.eggs` skill process, `Eggs.cs` (laying with catch-up from timestamps, `CollectEggs` reducer), client "Take eggs" offer. Server and client build; upgrade check passes against origin/main; 4 hens spawn on a fresh world. Hens draw as a 0.4 scale sheep until Modeling's hen exists.

## Open questions / blockers
- Numbers for the PM/Justin (D-075): 1 egg per hen per 1.5 game days (135 min real), cap 3, egg food 0.08, roast chicken 0.3 (mutton 0.4). 4 hens at (-8, 7).
- Needs: Modeling hen (peck, flap), Audio clucks and startled squawk, House building coop structure, item models for egg/feather/chicken (Showcase pending list).

## Next step
Open the PR, send green sha to PM; send Modeling/Audio/House building requests.
