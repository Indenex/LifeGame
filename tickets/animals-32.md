# animals-32: tick-cost guard in AnimalCheck
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-tick-guard-5gkntn
PR: (to open)

## Goal
PM suggestion after animals-31: AnimalCheck fails if the animal tick gets heavy again.

## Progress log
- 2026-10-17: AnimalCheck reads `/v1/metrics` (`reducer_wasmtime_fuel_used` and the duration count for `animal_tick`) over an 8 s window at the start of the run and fails above 90 M fuel per tick. Measured with the lone check character on a fresh world: 47.1 M and 51.2 M (two runs); 109.5 M with the per-chunk lookup sharing of animals-31 switched off, so the ceiling catches a half-lost optimisation. The PM's "about 3x today's" would have been 150 M, which that regression slips under; 90 M (about 1.8x) is still well above the 10% run-to-run spread. Fuel counts compute, not time, so CI machine speed does not matter. If the server serves no usable metrics, or fewer than 5 ticks run, the check says so and passes rather than flaking. Adds about 8 s to the run (about 65 s in all).

## Open questions / blockers
- If a later change legitimately makes the tick dearer (more species, new behaviour), raise `TickFuelCeiling` in tools/AnimalCheck/Program.cs with the measured new number in the ticket.

## Next step
Wait for CI on the PR, send the green sha to the PM.
