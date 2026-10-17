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
4. When something visible changes or a milestone lands, don't render it yourself: send a render request to the Render lane (D-059). Send render requests to the Render session listed in STATUS.md. The request names your ticket, branch and pushed commit sha, what to show and from where (place, time of day, what the player does), and a 2 to 3 line summary of what changed in plain words. **[WRITE]** add `Render requested <sha>` to the ticket's progress log. The Render lane captures images to /mnt/project-files/screenshots/ and sends them with your summary to the PM, who presents them to Justin. Justin reads one chat, the PM thread (D-034). Keep your own thread's status checklist current; post replies there only when Justin writes to you there. Don't wait for the render before opening or merging your PR (D-142), except for crucial look-and-feel choices (D-154): build only a rough prototype, send 2 to 4 written descriptions (plus renders if quick) to the PM right away, and work on other things until Justin picks.
5. An idea, a design question for Justin, or anything outside your lane: send it to the PM as 2 to 4 concrete options, recommended first, each with a one-line trade-off (D-059), **[WRITE]** add it under the ticket's `Open questions`, and keep working on a stated default. If it truly blocks you, set the ticket to `blocked` with exactly what you need.
6. When the PM relays a decision: **[WRITE]** note it in the ticket. The PM records it in `DECISIONS.md`.

## 2. Push and CI

1. Local build green: push.
2. **[WRITE]** record the CI run link in the ticket, and set `Next step` to wait for it.
3. When it finishes: **[WRITE]** update the ticket. Green: open or update the PR, set `in_review`, and ask the PM for the merge. Red: note the failing step in the ticket first, then fix. Never sit idle while a PR waits: send the PM the green head sha (the PM merges, D-119), then start your next ticket right away, stacked on your branch if needed.
4. Never idle (D-145): when your ticket is done or waiting, pick your next item from the milestones or the design docs, start it, and tell the PM.
5. Any random or flaky test failure gets an owner within the hour (Integration or Bug fixes); test bots use fixed seeds; never just re-run and move on (D-248).
5b. **Merging is a queue (docs/MERGE_QUEUE.md).** Send the PM the green sha as before; the PM adds the PR to GitHub's merge queue, which builds batches of 3 to 5 PRs, runs the checks once on the combined state and lands them in one push (one Build and one Release). A PR that fails in the queue is removed and comes back to you: fix it and tell the PM again. Nothing merges straight into `main` any more.
6. Don't push to a PR after reporting it green unless the PM asks; put ticket edits in your next code push (D-248).

## 3. Session-end checklist

- [ ] Everything committed and pushed (`git status` clean)
- [ ] Ticket: progress log current, `Next step` is one line a stranger could execute
- [ ] Open questions are in the ticket and have been sent to the PM
- [ ] Blocked? The ticket says `blocked` and names what's needed and from whom

## Render lane loop

1. Take render requests in the order they arrive. **[WRITE]** log each in your ticket (requester, sha).
2. Check out the requester's sha, run `tools/gen-bindings.sh`, build, start a local server and client, and capture what the request asks for (Chromium/Playwright or Godot screenshots; headless where possible).
3. Save images as /mnt/project-files/screenshots/<ticket>-<short sha>-<n>.png.
4. Send the PM: requester's summary, the image paths, and anything that looked wrong. Never judge design; report what you saw.
5. If the build or run fails, tell the requesting lane and the PM what failed; don't fix their code.
6. Justin does not read lane threads (D-246): images go to the PM, who sends Justin every screenshot and render directly.

## PM loop

- Session start: read `STATUS.md` and do `Next PM action`.
- After assigning a lane, getting Justin's answer, or a merge: **[WRITE]** update `STATUS.md` and append to `DECISIONS.md`.
- Render lane hands over images and a work summary: present them to Justin with the summary, plus any open choices for that work.
- Ideas and design questions go to Justin as 2 to 4 options, recommended first (D-059).
- Approve routine merges yourself once CI is green and they match `DECISIONS.md` (D-032). Bring Justin world wipes, design questions and anything irreversible or costly, batched and answerable in a word.
