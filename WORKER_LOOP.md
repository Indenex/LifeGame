# WORKER_LOOP

The loop every LifeGame lane session runs. Steps marked **[WRITE]** happen immediately; the session may end at any time.

## 0. Session start

1. Read `CLAUDE.md`, then `STATUS.md` on `main`.
2. Find your lane's row. Check out its branch and open its ticket in `tickets/` there. Go to the ticket's `Next step`.
3. If your lane has no open ticket, ask the PM for the next milestone. **[WRITE]** when you get it: create the ticket file with `Status: in_progress`, your branch, and a first progress-log line.
4. Run `git status` and `git log -5`. If a stopped session left uncommitted work, read the diff before touching anything, then commit it as `<ticket>: WIP recovered from interrupted session`.

## 1. Work

Repeat until the ticket's goal is met:

1. Do the smallest next piece of work.
2. Build and run the fastest relevant local check.
3. At every checkpoint:
   - **[WRITE]** commit and push
   - **[WRITE]** add a progress-log line and update `Next step` in the ticket
4. When something visible changes, post a short update with a screenshot in your thread for Justin.
5. A design question for Justin, or anything outside your lane: send it to the PM, **[WRITE]** add it under the ticket's `Open questions`, and keep working on a stated default. If it truly blocks you, set the ticket to `blocked` with exactly what you need.
6. When the PM relays a decision: **[WRITE]** note it in the ticket. The PM records it in `DECISIONS.md`.

## 2. Push and CI

1. Local build green: push.
2. **[WRITE]** record the CI run link in the ticket, and set `Next step` to wait for it.
3. When it finishes: **[WRITE]** update the ticket. Green: open or update the PR, set `in_review`, and ask the PM for the merge. Red: note the failing step in the ticket first, then fix.

## 3. Session-end checklist

- [ ] Everything committed and pushed (`git status` clean)
- [ ] Ticket: progress log current, `Next step` is one line a stranger could execute
- [ ] Open questions are in the ticket and have been sent to the PM
- [ ] Blocked? The ticket says `blocked` and names what's needed and from whom

## PM loop

- Session start: read `STATUS.md` and do `Next PM action`.
- After assigning a lane, getting Justin's answer, or a merge: **[WRITE]** update `STATUS.md` and append to `DECISIONS.md`.
- Bring Justin merges, design questions and anything irreversible, batched and answerable in a word.
