# claims-20: Test the town term logic
Lane: Claims, chat and PvP rules
Owner: Claims lane (Sonnet session)
Status: in_review
Branch: claude/claims-20-town-terms
PR: (see below)

## Goal
The hourly pass that ends office terms (ties, the Judge's tie-break, vacant seats, no votes) needs the server's own identity, so SocialTest could not reach it. Make it testable without changing what it does.

## Progress log
- 2026-10-17: the pass is now `SettleTerms(ctx)`, called by the hourly `TownTerms` (RequireServer, unchanged) and by a new admin-only `SettleTownTermsNow`. New admin-only `SetTownTerm(seconds)` (0 restores the defaults; a private `TownConfig` row) shortens term and vacancy for a playtest or a test. SocialTest, at the end of its town section: a tied vote leaves the seat empty until the term ends; the tie then goes to the longest-standing signer; the signers elect B as Judge; a second tie goes to the one the Judge voted for, even over the longer-standing signer; with no votes the holder stays on with a fresh term; terms restored. All pass locally on a fresh database (server and SocialTest; the client is untouched).

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR; send the PM the green sha. Then: powers for Marshal, Treasurer, Gatekeeper, Scribe and Elder need Justin's design (options to the PM).
