# Merge queue and auto-merge (integration-8)

What it does: a green PR is merged by GitHub itself. The queue builds the PR on top of the newest `main` (up to 5 PRs per batch), runs the same three checks, and merges the batch if they pass. Nobody has to wait for a green sha or a clear moment.

## Justin's clicks (once, repo admin)

1. **Settings → General → Pull Requests**: tick **Allow auto-merge**. Also tick **Allow squash merging** and untick the other two merge methods.
2. **Settings → Rules → Rulesets → New ruleset → New branch ruleset**.
   - Name: `main`. Enforcement status: **Active**.
   - Target branches: **Add target → Include default branch**.
   - Tick **Require status checks to pass**. Add exactly these three checks: `Client (Linux)`, `Client (Windows)`, `Server module and load test`. Leave "Require branches to be up to date" off (the queue does that).
   - Tick **Require merge queue**: merge method **Squash and merge**, maximum group size **5**, minimum group size **1**, wait time to meet minimum **2 minutes**, build concurrency **3**, status check timeout **60 minutes**.
   - Leave "Restrict deletions" and "Block force pushes" ticked. **Create**.

## Using it

- On a green PR, press **Merge when ready** (or have Claude call `enable_pr_auto_merge`). It joins the queue as soon as the required checks pass.
- A PR that fails in the queue is removed; the PRs behind it are rebuilt without it. Fix it and re-enable auto-merge.
- `world-wipe` PRs (D-129): do not enable auto-merge. The PM merges them by hand on the date Justin confirms. The queue run skips the upgrade check (it only runs on the PR itself), so a wipe PR in the queue would not be caught.
- Doc-only PRs still report the three checks (a few seconds each, nothing built), so the rule never blocks them.

## How the workflow behaves

- `build.yml` has a `merge_group` trigger; the queue's temporary `gh-readonly-queue/main/...` branches run the same jobs as a PR (smoke load test, no upgrade check).
- Pushes to `main` still run the full build (20 bots) and share one `build-main` concurrency group; the release picks only green **push** Builds of `main`, so queue branches are never released.
