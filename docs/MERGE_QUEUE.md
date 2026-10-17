# Merge queue (integration-8, integration-16)

Why: every merge to `main` used to start its own Build and Release, and PR runs competed with them for the same 20 free runner slots. With the queue, finished PRs are merged in batches: GitHub builds a batch of 3 to 5 PRs on top of the newest `main` (the `merge_group` event), runs the same checks once on the combined state, and lands the whole batch in one push. One push means one Build (module, Upgrade check, both clients) and one Release, about every 10 to 15 minutes, instead of one per PR.

## Justin's clicks (once, repo admin, about 2 minutes)

1. **Settings → General → Pull Requests**: tick **Allow auto-merge**. Keep **Allow merge commits** ticked (history stays "Merge pull request #N").
2. **Settings → Rules → Rulesets → New ruleset → New branch ruleset**.
   - Name `main`. Enforcement status **Active**. Target branches: **Add target → Include default branch**.
   - Tick **Require status checks to pass** and add exactly: `Client (Linux)`, `Client (Windows)`, `Server module and load test`. Leave "Require branches to be up to date" off (the queue does that).
   - Tick **Require merge queue** with: merge method **Merge commit**; build concurrency **1**; maximum group size **5**; minimum group size **3**; wait time to meet minimum group size **10 minutes**; **Require all queue entries to pass required checks** on; status check timeout **45 minutes**.
   - Bypass list: add the repository **Admin** role (the PM uses it to hand-merge a `world-wipe` PR).
   - Leave "Restrict deletions" and "Block force pushes" ticked. **Create**.

After that, nobody but the bypass role can merge straight into `main`.

## Using it

- Lanes: send the green sha to the PM, as before (D-119). The PM adds the PR to the queue (`enable_pr_auto_merge` on it; it joins as soon as the required checks pass) instead of merging.
- A batch forms when 3 PRs are waiting, or when 10 minutes pass with fewer. The batch is built on the newest `main`, so a PR that conflicts with another in the same batch fails there.
- A PR that fails in the queue is removed and the rest are rebuilt without it; the PR goes back to its lane. Fix it, push, and it is queued again.
- `world-wipe` PRs (D-129) are not queued: the queue run cannot see the label. The PM merges them by hand with the bypass role on the date Justin confirms.
- Doc-only PRs still report the three checks (a few seconds each, nothing built), so the rule never blocks them.

## How the workflow behaves

- PR runs: unchanged, the full checks (lighter PR runs are possible later, since the queue run tests the combined state).
- `merge_group` runs (branches `gh-readonly-queue/main/...`): the same jobs as a PR, including the Upgrade check of the batch over the queue's base commit, so a batch that would need a world wipe is stopped before it merges. Queue branches are never released.
- Push to `main` (the batch landing): module, Upgrade check (against the push's `before` commit, so the whole batch is covered) and both client exports, then the Release. No gameplay checks: they just ran on the batch. One `build-main` concurrency group, so a newer push replaces a queued Build.
