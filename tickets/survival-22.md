# survival-22: SurvivalCheck (end-to-end survival regression check)
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_review
Branch: claude/survival-check
PR: (to open)

## Goal
The thirst ease (survival-14), the food data (survival-17/18/19) and the waterskin (survival-20) were only covered by builds and the upgrade check. `tools/SurvivalCheck` connects one fresh bot to a published server and checks, in a few seconds: a new character's thirst falls at 0.8 of the usual rate for the first hour and hunger is not eased; berries give their food and water; stew is a good meal (well-fed buff) and the coil pot comes back at its quality; a waterskin of water and a pot of water are drunk and the empty container comes back at its quality; eating a stick and drinking from a stick are refused. Items come from the admin give, so the tool makes its bot an admin by calling `spacetimedb-cli call <db> --server <server> make_admin '"<name>"'` (the publisher's identity), so that CLI must be on PATH. Not covered: filling a pot or skin at the water (needs a walk to the shore; CraftCheck/ClayCheck own pot filling) and roasting.

## Progress log
- 2026-10-17: written; passes against a fresh local world (all 10 checks) in a few seconds.

## Open questions / blockers
- CI: Integration owns `.github/workflows/build.yml`. Ask them to run `cd tools/SurvivalCheck && dotnet run -c Release` next to SpawnCheck in the load/spawn/social job (it needs `spacetimedb-cli` on PATH and `--server local`, as that job's other steps use).

## Next step
Open PR, wait for CI, send the green sha to the PM (session_01CvoSQGRe3vDvDXWQu2qruU) and ask Integration (session_015g3pLB5eJ4qPrXAwJUmkYT) to add the workflow step.
