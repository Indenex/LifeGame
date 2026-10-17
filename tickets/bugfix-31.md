# bugfix-31: Test-bot nondeterminism audit
Lane: Bug fixes
Owner: Bug fixes thread (with Integration, who is splitting the server job in integration-10)
Status: in_review
Branch: claude/bugfix-31

## Goal
Every random CI failure has an owner within the hour and a root-cause fix (Justin's flaky-test rule). Audit BuildTest, CraftCheck, SocialTest and ClayCheck for random spawns and timing waits; propose fixes.

## Findings so far (2026-10-17, first grep pass)
- None of the four tools use a seeded RNG. Only SocialTest uses `Random.Shared` (a random name suffix, harmless).
- The real randomness is server-side: new players spawn at `ctx.Rng` positions in `PlayerConnected` (Reducers.cs), so each run starts the bots at different spots, next to different trees and rocks. That is the likely source of "fire sits on boulder", "Out of reach" and wedged-walker failures.
- Timing: fixed `Thread.Sleep(500)` in SocialTest (lines ~185-189) and polling loops with stopwatch deadlines in CraftCheck/ClayCheck/BuildTest depend on server load; CI machines vary.

## Proposal
1. A test-only deterministic spawn: bots call a seed or choose a fixed start (e.g. an admin/test reducer, or a spawn derived from the identity) so every run starts at the same spot.
2. Replace fixed sleeps with wait-until-condition helpers with generous deadlines.
3. On any failure print the bot's position, the spawn and the server pace so a failure can be reproduced.

## Next step
Finish after #335/#399 merge: confirm with Integration which of 1-3 fits integration-10, then implement.

## Progress log
- 2026-10-17: CraftCheck, ClayCheck, BuildTest and SocialTest now walk to the origin before they start (bots A at 0,0 and B at 1,0), so every run begins from the same place instead of a random spot in the spawn clearing. Local: CraftCheck, ClayCheck, BuildTest (335 s) pass; SocialTest passes with the #435 presence fix.
- Two real flake causes found and fixed on the way: BuildTest bots penned inside the hut by the new server walls (#394), and the presence sweep marking idle check bots offline (#435).
- Still open: wait-until-condition helpers for SocialTest's fixed 500 ms sleeps, and failure output with position/pace.
