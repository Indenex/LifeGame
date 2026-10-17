# animals-31: animal tick cost
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-tick-cost-5gkntn
PR: (to open)

## Goal
PM item (a): measure the animal tick and trim it if heavy. Done when the tick is measurably cheaper with the same behaviour.

## Progress log
- 2026-10-17: measured on a local server with 5 LoadTest bots connected and the real world (42 animals, 10 flocks): mean 39 to 53 ms per 500 ms tick (7 to 10% of the budget), about 184M wasm fuel per tick. Method: `/v1/metrics` counters `reducer_wasm_time_usec`, `reducer_wasmtime_fuel_used` and the reducer duration count for `animal_tick`, sampled before and after a 25 s LoadTest; fuel is the steadier number (time varies 40 to 53 ms run to run). Fixed overhead (watchers, flock loads) is about 5 ms; flock thinking is the rest, about 1 ms per animal.
- Cause 1: AnimalStepIntoObstacle worked tree and boulder positions out from noise for 9 cells on every animal step, about 47% of the compute. They never change, so each cell is now worked out once and kept (cap 100k cells); only the live felled/dug test stays on the tables. Fuel 9.96G to 5.97G over the run.
- Cause 2: each animal step and each fire check re-read the Structure, Fire and Kiln chunk indexes (about 30 lookups per step). During the tick they are now read once per chunk and shared (flag `AnimalTickScope`, cleared in a finally; StructuresNear in Building.cs gained the cached branch, nothing else there changed). Fuel to 2.55G, mean tick 16 ms.
- Result: 53 ms to 16 ms per tick (about 74% less compute) for the same world. AnimalCheck passes on it.

## Open questions / blockers
- Cost still scales with awake animals (about 0.3 ms each now). If worlds grow to hundreds of awake animals, the next step is thinking only every other tick for far-off flocks; that changes how animals look at distance, so it is a design question for the PM.

## Next step
Wait for CI on the PR, send the green sha to the PM.
