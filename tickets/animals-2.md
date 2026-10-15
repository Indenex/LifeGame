# animals-2: Shearing, then meat and hide
Lane: Animals
Owner: Animals thread (session session_01XS7M1yLy2fLaFdE3HWnisM)
Status: in_progress
Branch: claude/animals-sheep-5gkntn
PR: (opening, shearing part)

## Goal
Shearing: Use (G) on a calm sheep within reach while carrying a Cutting tool gives Raw wool (item 193) at the tool's quality, capped by skill. The sheep visibly turns thin and pink, and the fleece regrows over about 3 in-game days. Nervous sheep won't stand still, so you sneak up first (D-026).
Then (D-039): killing with a weapon or cutting tool turns into a short chase; butchering the carcass with a cutting tool drops Raw mutton (190, Crafting's) and Sheep hide as world items; flocks regrow a lamb about every in-game day and never drop below 2. Calm and respectful, not gory (D-036).

## How it works (shearing)
- `server/spacetimedb/Shearing.cs`: `ShearSheep(animalId)` checks reach, fleece >= 80, nervousness <= 0.15 and not running, best Cutting tool; quality = min(tool quality, skill cap for process `animal.shear`). Full fleece gives 2 wool, otherwise 1.
- `Animal.Fleece` (0-100, default 100) and `AnimalMind.ShornAt` were appended with defaults, so existing worlds migrate without a wipe. Regrowth is computed from ShornAt in steps of 5 during awake ticks.
- Client: `Herd` binds G as "shear" until the UX Use dispatcher (#17) lands; `Sheep` shows a pink shorn body inside the fleece, scales the fleece with Fleece, and puffs wool tufts when shorn.

## Progress log
- 2026-10-15 22:55: shearing works locally (no tool -> "need something sharp", sheared -> 2 wool, re-shear -> "hasn't grown back"). Screenshots screenshots/animals-03-*.png.
- 2026-10-15 23:30: merged main; shearing is now a Use action ("Shear", priority 15, offered only next to a sheep with grown wool) and Herd no longer binds G. A flock bolting within 10 m asks for the sneak hint.

## Open questions / blockers
- Skills: `animal.shear` maps to no skill yet, so wool quality is capped at the novice cap (35). Skills lane to map it (e.g. a husbandry skill).

## Next step
PM merges #23 once CI is green; then killing/butchering per D-039 (animals-3).
