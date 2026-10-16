# bugfix-9: BuildTest no longer checks a craft before it lands
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: in_review
Branch: claude/bugfix-lyle6o
PR: #59 (rides with bugfix-8, per the PM)

## Goal
CI's building check stops failing at random on "twisted cordage for the basket". On PR #59's run 38841747143 it saw Cordage:1 Fiber:3: the server had taken fiber for both twists, but the test checked before the second twist finished. Bot.Craft in tools/BuildTest/Program.cs waited only while MyCraftJob had a row, so if the job row hadn't reached the client yet it stopped waiting at once. Done means Bot.Craft waits until the job has come and gone, or the made item has turned up, with the same timeout. Only the test tool changes; Justin sees nothing in game.

## Progress log
- 2026-10-16 02:55: PM assigned it (Building lane won't touch tools/BuildTest/Program.cs).
- 2026-10-16 03:05: fix written; locally the basket check passed 3 of 3 and the full building test ("both") passed in 200 s against a fresh module.

## Open questions / blockers
- (none)

## Next step
Wait for CI on PR #59 and send the PM the green sha.
