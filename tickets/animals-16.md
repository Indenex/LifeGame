# animals-16: Sheep and deer keep out of tree trunks and boulders
Lane: Animals
Owner: Animals lane (Sonnet)
Status: in_review
Branch: claude/animals-collision-5gkntn
PR: (see below)

## Goal
Sheep and deer no longer walk through tree trunks or boulders (Bug fixes bugfix-23 / #228 ask). Server side only, in `AnimalStepBlocked` (AnimalFires.cs): a step may not end inside a trunk (0.35 m + 0.3 m body) or boulder (Rocks.Radius + 0.3 m) unless it moves the animal away from it, so an animal standing in one can walk out. Felled or dug-out trees don't block. No schema change.

## Progress log
- 2026-10-16: written, not built locally (no dotnet in this container); CI is the check.

## Open questions / blockers
- None.

## Next step
Wait for CI on the PR; send the PM the green sha.
