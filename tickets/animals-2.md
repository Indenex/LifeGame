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

## Open questions / blockers
- Skills: `animal.shear` maps to no skill yet, so wool quality is capped at the novice cap (35). Skills lane to map it (e.g. a husbandry skill).
- Move G shearing onto the UX Use dispatcher when #17 is on main.

## Next step
Open the shearing PR; then start killing/butchering per D-039.
