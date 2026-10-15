# CLAUDE.md

Rules for every Claude session working on LifeGame, on any account. Read this first, then `STATUS.md`.

Working state does not live in chat. It lives in the repo, so a session on either of Justin's Claude accounts can pick up where another stopped:

- `STATUS.md`: who owns which lane, which ticket and branch each lane is on, open questions, and what the PM does next. Start here.
- `tickets/`: one file per lane milestone. The owning lane keeps it current on its own branch.
- `DECISIONS.md`: append-only log of what Justin and the PM decided, and why.
- `WORKER_LOOP.md`: the step-by-step loop every lane session runs.

Design scope: https://claude.ai/code/artifact/8064d6ad-709a-42b9-8136-ad85e16111a8. Game design doc (skills, skill trees, materials, crafting chains): https://claude.ai/code/artifact/50ab4888-c6e0-44b1-9408-f5a02f887c8f. Server rules for SpacetimeDB are in `server/CLAUDE.md`.

## Roles

- **Justin** owns the game. He wants to be very involved in design: frequent progress updates with screenshots, and design questions (how something looks, feels or plays, rules and numbers players notice) brought to him rather than decided silently.
- **PM** (Project Manager) assigns lanes and tickets, keeps `STATUS.md` accurate, answers technical questions itself, takes design questions, merges, spending and anything irreversible to Justin, and records his answers in `DECISIONS.md`.
- **Lane sessions** each own one lane (see `STATUS.md`) and work one ticket at a time on their own branch. Questions go to the PM; meanwhile keep working on a stated default.

## The no-handoff rule

Any session can stop at any moment (usage limits, account switch). So:

1. **Write state the moment it exists.** A decision goes in `DECISIONS.md` or the ticket now; progress goes in the ticket now. Not at the end.
2. **Commit and push at every checkpoint**: build passes, a sub-task finishes, or before trying something risky. Unpushed work is what gets lost.
3. **Write every ticket for a stranger** with no chat history. Its `Next step` line must always be something a fresh session can execute.
4. Before stopping for any reason, run the session-end checklist in `WORKER_LOOP.md`.

## Lanes and file ownership

Each lane works in its own files so lanes don't collide:

- Server code in `server/spacetimedb/<Feature>.cs` as `partial class Module`.
- Client code under `client/scripts/<feature>/`.
- Shared files (`client/scripts/Main.cs`, `client/scripts/Hud.cs`, `server/spacetimedb/Tables.cs`, `shared/World.cs`) only get small hooks: add a node or a call, no rewrites. The Crafting lane is the integration owner for these, CI and builds.
- Items are `LifeGame.Shared.Item` ids in `shared/Items.cs`; process ids are dotted strings in `shared/Processes.cs`. Add new ones with new numbers; never renumber or rename.
- After merging main, regenerate `client/module_bindings/` with `spacetime generate` (see `README.md`). Never hand-merge generated files.

## Git

- Cloud sessions push to the branch the harness assigns (usually `claude/...`). Record the real branch name in your ticket and `STATUS.md` row.
- Commit messages lead with the ticket id: `crafting-1: add hand-drill fire`.
- Never force-push a shared branch, never commit to `main` directly. Each milestone merges through a PR, and merging needs Justin's OK through the PM.
- Commit before any checkout, rebase or merge. Never stash and walk away.

## CI

- `.github/workflows/build.yml` builds the server module, checks bindings are current, runs a short load test, and exports Linux and Windows clients.
- Platforms are Linux and Windows only for now.
- Build locally where you can before pushing; don't push just to see if CI passes.
- Write the CI run link and result in your ticket so the next session doesn't re-run it.

## Ticket file format (`tickets/<lane>-<n>.md`)

```
# crafting-1: Title
Lane: Crafting
Owner: Crafting thread (or the account/session that holds it now)
Status: todo | in_progress | blocked | in_review | done
Branch: claude/...
PR: #NN (once opened)

## Goal
One paragraph. What "done" means, concretely, including what Justin will see.

## Progress log
- 2026-10-15 20:40: started; hand-drill fire works locally
- 2026-10-15 21:10: pushed abc1234, CI run <link> green

## Open questions / blockers
- (none, or exactly what is needed and from whom)

## Next step
One line. The very next action, even if it is "wait for CI run <link>".
```
