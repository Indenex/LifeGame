# integration-10: Smarter, parallel CI
Lane: Integration
Owner: Integration thread
Status: in_review
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

## Progress log
- 2026-10-17: workflow written (PR). After numbers to be filled in from the PR's own run.

## Next step
Compare the PR's run time with the before figures, fix anything the run shows, then send the PM the green sha. Later: Godot .godot import cache.
