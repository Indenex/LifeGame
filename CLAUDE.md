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
- **Feature lanes** (designers/workers) each own one feature lane (see `STATUS.md`) and build game features, one ticket at a time on their own branch. Questions go to the PM; meanwhile keep working on a stated default.
- **Integration lane** is a dedicated worker that owns integration, CI and builds: the shared files, `.github/workflows/`, build and bindings tooling, the Use dispatcher, and merge-readiness of PRs (resolving cross-lane conflicts, keeping `main` building). Feature lanes do not edit CI; they ask Integration. Integration also executes routine merges (D-095): send it your green sha.
- **Audio lane** owns all sound and music (D-079). Other lanes send it audio requests and never make sounds themselves.
- **Environment** and **Atmosphere** lanes together own cosy immersion: Environment builds the world (terrain, plants, rocks, water edges), Atmosphere sets light, sky, weather and mood (D-078).
- **Render lane** is a dedicated worker that makes everything Justin looks at: screenshots, short clips, mockups. Other lanes never render their own work; they send a render request (see `WORKER_LOOP.md`) and the Render lane sends the images plus the requester's work summary to the PM, who presents them to Justin (D-059).

## Ideas become choices (D-059)

Whenever the PM or any worker has an idea (a feature, a look, a number, a different approach), it reaches Justin as a short question with 2 to 4 concrete options, the recommended one first and marked, each with a one-line trade-off, answerable in a word. Workers send ideas to the PM in that shape; the PM batches them and asks Justin. Until he answers, keep working on the recommended option.

## Models (D-058)

- **Opus** for any session or agent that writes or designs code: the PM, every lane session, and coding subagents.
- **Sonnet** for small, basic tasks: searching, reading logs, summarizing, ticket and status bookkeeping, mechanical one-file edits, running checks.
- A lane session acts as **coordinator** for its own work: it breaks a ticket into small, clearly specified tasks and hands the basic ones to Sonnet subagents (Agent tool `model: "sonnet"`), keeping design and non-trivial code with Opus (`model: "opus"`). The coordinator reviews each result before committing it.
- New sessions started for a lane (e.g. `create_session`) use Opus.

## Design principles

- **Start low, grow slowly (D-075).** Every number (gear bonuses, skill gains, yields, speeds, capacities) starts low and rises slowly; the curve is quicker through about the first 15% of progression, then flattens. The game is meant to take a long time. Propose numbers to the PM with where they sit on that curve.
- **Fun first, some realism (D-081).** Survival is light: needs nudge players along, never punish; no death spirals. Aesthetics and a world to get lost in come first; everything a player makes should look real on the ground, in hand and in the inventory.
- **Lanes never edit `STATUS.md` on their branches.** Send STATUS changes to the PM.

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
- Shared files (`client/scripts/Main.cs`, `client/scripts/Hud.cs`, `server/spacetimedb/Tables.cs`, `shared/World.cs`) only get small hooks: add a node or a call, no rewrites. The Integration lane owns these, CI and builds.
- Items are `LifeGame.Shared.Item` ids in `shared/Items.cs`; process ids are dotted strings in `shared/Processes.cs`. Add new ones with new numbers; never renumber or rename.
- `client/module_bindings/` is generated, not committed. Run `tools/gen-bindings.sh` after cloning, after merging main, and after changing the server module, before building the client or tools.

## Git

- Cloud sessions push to the branch the harness assigns (usually `claude/...`). Record the real branch name in your ticket and `STATUS.md` row.
- Commit messages lead with the ticket id: `crafting-1: add hand-drill fire`.
- Never force-push a shared branch, never commit to `main` directly. Each milestone merges through a PR. Routine merges (CI green, PR matches decisions already in `DECISIONS.md`, D-032) are executed by the Integration lane (D-095); Justin approves anything that wipes his world, makes an undecided design change he will notice, or costs money.
- Commit before any checkout, rebase or merge. Never stash and walk away.

## CI

- `.github/workflows/build.yml` builds the server module, generates the bindings, runs the load test (5 bots for 5 s on pull requests, 20 for 20 s on main) and the gameplay checks, and exports Linux and Windows clients. NuGet, SpacetimeDB and Godot downloads are cached.
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
