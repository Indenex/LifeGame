# crafting-4: CI upkeep (Node 24 actions, pinned runner)
Lane: Crafting (integration owner)
Owner: Crafting thread
Status: in_progress
Branch: crafting-ci-upkeep
PR: (not yet)

## Goal
CI stops warning that actions run on deprecated Node 20, and won't silently move to Ubuntu 26 when ubuntu-latest migrates (from 2026-10-19). actions/checkout, setup-dotnet, cache and upload-artifact move to their first Node 24 majors (v5, v5, v5 and v6; upload-artifact v5 was still Node 20). Both jobs run on ubuntu-24.04. Justin sees nothing change except green CI with no deprecation annotations; artifacts keep their names.

## Progress log
- 2026-10-15 23:45: bumped the four actions to v5 and pinned the runner to ubuntu-24.04.
- 2026-10-15 23:50: run 38824537122 green, caches hit; upload-artifact v5 still warned about Node 20, so moved it to v6 (same inputs, node24).

## Open questions / blockers
- (none)

## Next step
Push, open the PR, and check the run shows no Node 20 annotation and the caches still hit.
