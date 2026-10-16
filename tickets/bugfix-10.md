# bugfix-10: SocialTest copes with a claim that already fell
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: in_review
Branch: claude/bugfix-lyle6o
PR: #59 (rides with bugfix-8 and bugfix-9, per the PM)

## Goal
CI's claim and PvP check stops crashing at random. CI sets the claim lifetime to 30 s; on PR #59's run 38843591908 the claim was finished at 03:12:36 and striking A down took until 03:13:09, so the claim had already decayed when the decay block read `a.MyClaim()!` (tools/SocialTest/Program.cs) and it threw a NullReferenceException. Done means the decay block accepts a claim that already fell and goes on to check the marker, stash spill and the rest as before. Only the test tool changes; the Claims lane leaves this to Bug fixes.

## Progress log
- 2026-10-16 03:15: PM assigned it; patch pushed onto PR #59.

## Open questions / blockers
- (none)

## Next step
Wait for CI on PR #59 and send the PM the green sha.
