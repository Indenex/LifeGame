# animals-29: AnimalCheck tool
Lane: Animals
Owner: Animals thread
Status: in_progress
Branch: claude/animals-checks-5gkntn
PR: (to open)

## Goal
PM approved option 1 (2026-10-17): a `tools/AnimalCheck` bot that proves the animal server rules in CI. Scope as built: Def data invariants (`shared/Animals.cs`) plus reducer refusals for a missing animal and an animal out of reach (strike, shoot, collect eggs). Not covered, needs placed animals or claims and a weapon: arrows blocked by walls, swing pacing, eggs and leather refused to non-members. Those wait for a way to place a hen in a claim in a test (ask Integration).

## Progress log
- 2026-10-17: wrote the tool; no dotnet in the cloud container, so first compile and run is CI's. Not wired into the workflow: Integration (session_015g3pLB5eJ4qPrXAwJUmkYT) adds the step, ~5 s.

## Open questions / blockers
- Written without a local compile; expect a CI round to fix compile errors.

## Next step
Send Integration the tool name and runtime; open the PR; send the green sha to the PM.
