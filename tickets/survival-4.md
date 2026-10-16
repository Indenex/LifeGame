# survival-4: Eat mutton, best food on X, ui-4 follow-ups
Lane: Survival needs
Owner: Survival thread
Status: in_progress
Branch: claude/survival-needs-g7yrhi (restarted from main after #39 merged)
PR: (not yet)

## Goal
Roast mutton fills 40% of hunger (Justin, D-039). Burnt mutton fills 10% (our default), and raw mutton can't be eaten yet. X eats the best food you carry: the most filling one that doesn't waste much, otherwise the smallest. Once UX's ui-4 (#38) is on main: fade the needs rings while Panels.Open(), have forage pass a Use Target and drop survival.forage_facing, and call Hints.Done("eat") where it fits.

## Progress log
- 2026-10-16 00:30: Needs.Nourishment, Edible and BestFood are now in shared/Needs.cs, and the server's Eat uses them. X picks with BestFood; raw mutton gets "needs cooking first". Tested on a local server: roast took food from 0.80 to 1.0, burnt was eaten, raw was refused.
- 2026-10-16 00:40: ui-4 follow-ups: forage passes a Use Target and survival.forage_facing is gone; the needs rings fade while Panels.Open(). No Hints.Done needed, since eating only happens through X.

## Open questions / blockers
- Burnt mutton at 10% is our default; Justin hasn't set it.
- (none for ui-4: done locally)

## Next step
Wait for CI on the survival-4 PR, then send the green sha to the PM.
