# animals-1: Sheep with simple AI
Lane: Animals
Owner: Animals thread (session session_01XS7M1yLy2fLaFdE3HWnisM)
Status: in_review
Branch: claude/animals-sheep-5gkntn
PR: #13

## Goal
A flock of sheep that wanders, grazes, follows each other, shies away from a fast-approaching player and calms near a still one. Server-authoritative at a low decision rate (1 to 2 Hz) with client smoothing; only in subscribed chunks. Charming, Valheim-style look with walk, graze and startle animations. Done when sheep roam a build and react the same on two players' screens.

## How it works
- Server `server/spacetimedb/Animals.cs`: public `animal` table (position, heading, state Graze/Idle/Walk/Run, chunk), private `animal_mind`, `flock`, `animal_player_seen`. `AnimalTick` runs every 500 ms but only for flocks with an online player within view range + 1 chunk; a row is written only when a sheep moves or changes state. `EnsureAnimals` (called from `PlayerConnected`) starts the timer and places 4 flocks the first time, so old worlds get sheep too. The first flock lives in the spawn clearing.
- Fear: players closing in faster than `World.SneakSpeed + 0.6` m/s scare sheep, panic spreads to flock-mates, they bolt, stop and stare, then calm. Still players calm them; a calm sheep may walk up to a player who has stood still for 4 s.
- Client `client/scripts/animals/`: `Herd` binds to the `animal` table; `Sheep` builds a procedural woolly mesh and glides between server positions (0.6 s glide per 0.5 s tick), with walk, run, startle hop, graze and look-around animations.
- Sneak (D-026): hold Shift to walk at `World.SneakSpeed` (1.6 m/s); hook in `Main.Walk`.

## Progress log
- 2026-10-15 21:00: server AI and client sheep working locally; scripted-player test shows flee, regroup and curiosity.
- 2026-10-15 21:30: first screenshots posted (screenshots/animals-01-*.png). D-026 sneak and D-027 fleece colors decided.
- 2026-10-15 22:00: merged main, added sneak; pushed. CI run https://github.com/Indenex/LifeGame/actions/runs/38813307014 green.
- 2026-10-15 22:35: two clients captured at the same wall-clock moments while a third player charged the flock; both show the same bolt (screenshots/animals-02-*.png). PR ready, merge asked of the PM.

## Open questions / blockers
- Camera: tree canopies often hide the player and nearby sheep in the woods. Needs fading canopies near the camera (not this lane; reported to the PM).

## Next step
Wait for Justin's merge OK on PR #13 via the PM; then the PM picks the next animals milestone (shearing/wool, meat and hide, more species).
