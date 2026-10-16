# bugfix-12: "New in your journal" repeats on every pickup
Lane: Bug fixes
Owner: Bug fixes thread
Status: done
Branch: claude/bugfix-lyle6o
PR: #68

## Goal
A fresh player sees "New in your journal: Foraging" and hears the discovery chime once, the first time they forage, not on every stick they pick up afterwards.

Cause: `my_journal` is a view without a primary key, so when the server bumps `TimesPerformed` the client gets a delete and an insert, and `Journal.Learned` ran on the insert as if it were new. Fix: `Journal` keeps a set of process ids it has already seen and only announces ids not in it.

## Progress log
- 2026-10-16: reproduced on a fresh db (34th forage still toasted, screenshot bugfix-12-before-journal-toast-on-34th-pickup.png); fixed in client/scripts/journal/Journal.cs; 35th forage shows no toast (bugfix-12-after-no-toast.png).

## Open questions / blockers
- (none)

## Next step
None; merged.
