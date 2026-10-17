# animals-29: AnimalCheck tool
Lane: Animals
Owner: Animals thread
Status: in_progress
Branch: claude/animals-checks-5gkntn
PR: (to open)

## Goal
PM approved option 1 (2026-10-17): a `tools/AnimalCheck` bot that proves the animal server rules in CI. Stage 1 (#782): Def data invariants plus refusals for a missing animal and an animal out of reach. Stage 2 (PM, same PR): admin-only `AdminSpawnAnimal` (`server/spacetimedb/AdminTest.cs`, gated like AdminGive; the animal has no flock so the tick leaves it in place), and AnimalCheck uses AdminGive for a bow, arrows and a spear: an arrow through a tree trunk is stopped, a clear shot flies, a second swing straight after the first does nothing even on another animal, spear strikes and egg taking need reach. Stage 3, still to do: claim egg/leather rules for non-members (needs a second bot, a second claim via BuildTest's Bot.FindRingSite, and an admin hook to hang a hide).

## Progress log
- 2026-10-17: wrote the tool. Installed dotnet to /tmp/dn (dotnet-install.sh works) and used /root/stdb: built, ran against a local server, all ok; breaking the swing pause and the arrow check each makes the tool fail. Not wired into the workflow: Integration (session_015g3pLB5eJ4qPrXAwJUmkYT) adds the step, ~5 s.

## Open questions / blockers
- Stage 3 (claims) not started.

## Next step
Send Integration the tool name and runtime; open the PR; send the green sha to the PM.
