# integration-10: Smarter, parallel CI
Lane: Integration
Owner: Integration thread
Status: in_review (PR #413 merged; follow-up on claude/integration-craft-faster)
Branch: claude/integration-parallel-ci
PR: (see GitHub)

## Goal
Faster Builds with the same coverage and the same three required check names (`Server module and load test`, `Client (Linux)`, `Client (Windows)`).

## Before (PR #403, run 38989607035, 1 server job): 16 min 38 s wall
Server job 16:20 = setup+module build 0:45, bindings 0:18, load+BuildTest+SpawnCheck 6:37, upgrade check 1:12, CraftCheck 5:52, SocialTest 1:33. Clients 2:03 (Linux), 1:29 (Windows) in parallel.

## What changed
- `changes` job: path rules decide what runs. Docs only: nothing. Client-only: no server tests. Server- or tools-only: no Windows export. Workflow or unknown root files: everything. Main pushes, manual runs: everything.
- `module` builds the module once and shares the wasm (`LifeGame-Server`, as before, used by the release) and the generated bindings (`module-bindings`).
- Parallel test jobs, each on its own fresh local server: `load` (LoadTest, SpawnCheck, SocialTest), `build-test`, `craft-test`, `upgrade` (PRs only).
- `Server module and load test` is now a gate job that passes when all of those passed or were skipped (and fails if the change detection failed).
- Composite actions `.github/actions/setup-stdb` and `start-server` replace the copied setup steps.

## After (PR #413, run 38991156084): 8 min 46 s wall (was 16 min 38 s)
Crafting check 7:31 (longest), Building check 6:11, Load/spawn/social 3:10, Upgrade 1:33, module build 1:00, clients 1:31 (Linux) and 1:56 (Windows). Client Export step (with the Godot import) is 17 s of that, so an import cache is not worth its stale-cache risk.

## Progress log
- 2026-10-17: follow-up: CraftCheck's 60 s wait for the first fire to burn down now runs last, so the rest of the check passes the time (about -1 min). BuildTest (6:11) becomes the long pole.
- 2026-10-17: workflow written (PR). After numbers to be filled in from the PR's own run.

## Next step
Compare the PR's run time with the before figures, fix anything the run shows, then send the PM the green sha. Later: Godot .godot import cache.

## integration-13: main builds and releases first (Justin)
`.github/workflows/priority.yml` ("Main priority"): GitHub has no job priority, so while a push-triggered Build or any Release is queued or running, queued and in-progress pull_request Build runs are cancelled (job summary: "paused for a main build; re-runs automatically"). When main is idle (after each finished Build or Release, and on a 15-minute schedule) the newest cancelled run per open PR head is re-run. Merge-queue and manual runs and Release runs are never touched; PR runs a newer push replaced are left alone. Needs `actions: write`. WORKER_LOOP.md step 5b tells lanes a cancelled run is not red.
## Update 2026-10-17: main builds and ships only (Justin approved)
- Push to main runs the module build, the Upgrade check (now also on main; the merged PR's `world-wipe` label is looked up through the API so a wipe merge doesn't go red) and the clients, plus the `server` summary job. The load, Building and Crafting checks run on PRs only, because they already ran before the merge.
- Draft PRs skip those three checks (module, upgrade and clients still run); `ready_for_review` is a pull_request trigger so the full set runs when the PR is marked ready.
- A ticket/docs-only push already skips every heavy job (the `changes` filter), on PRs and on main.
- Load test on PRs stays 5 bots for 5 s; main no longer runs the 20 bots / 20 s one.
