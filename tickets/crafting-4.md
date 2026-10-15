# crafting-4: CI upkeep (Node 24 actions, pinned runner)
Lane: Crafting (integration owner)
Owner: Crafting thread
Status: in_progress
Branch: crafting-ci-upkeep
PR: (not yet)

## Goal
CI stops warning that actions run on deprecated Node 20, and won't silently move to Ubuntu 26 when ubuntu-latest migrates (from 2026-10-19). actions/checkout, setup-dotnet, cache and upload-artifact move from v4 to v5 (the first Node 24 major of each; later majors only changed internals). Both jobs run on ubuntu-24.04. Justin sees nothing change except green CI with no deprecation annotations; artifacts keep their names.

## Progress log
- 2026-10-15 23:45: bumped the four actions to v5 and pinned the runner to ubuntu-24.04.

## Open questions / blockers
- (none)

## Next step
Push, open the PR, and check the run shows no Node 20 annotation and the caches still hit.
