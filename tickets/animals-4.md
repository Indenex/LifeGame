# animals-4: Lambs follow their mothers
Lane: Animals
Owner: Animals thread (session session_01XS7M1yLy2fLaFdE3HWnisM)
Status: in_progress
Branch: claude/animals-sheep-5gkntn
PR: (to open)

## Goal
Lambs stay at their mother's side: they trail a step behind her when she walks, graze when she grazes, look about when she stops, and skip now and then. Pure immersion (D-036), cheap on the server. Done when a screenshot shows lambs beside their ewes in a flock.

## How it works
- Server `server/spacetimedb/Lambs.cs`: `AnimalMind.MotherId` (new column, default 0). Lambs born from `FlockRegrow` get their mother's id. Each tick a lamb with a living mother runs `LambDecide` instead of the flock logic: re-aim at a spot 0.9 m behind and 0.55 m to its side of her while she moves, trot after her past 2.2 m, copy Graze/Idle when alongside. Fear works as usual; a fleeing lamb runs the way its mother runs. A lamb without a mother (killed, or born before this) adopts the nearest grown sheep; at growth 100 it lets go. Lambs never become flock leader while a grown sheep remains.
- Client `Sheep.cs`: lambs at rest give a small skip every 5 to 14 s.
- Schema additive (one `[Default]` column).

## Progress log
- 2026-10-16 01:10: server and client written; both build.

## Open questions / blockers
- Lamb voices: Atmosphere's SheepVoices could bleat higher for lambs (Animal.Growth < 100). Suggest to Atmosphere via the PM.

## Next step
Test locally with a short birth timer (temporary), take screenshots, open the PR.
