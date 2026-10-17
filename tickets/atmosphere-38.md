# atmosphere-38: Grit dust when pounding
Lane: Atmosphere
Owner: World atmosphere lane (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: done
Branch: claude/atmosphere-38-pound-dust
PR: #733

## Goal
Animation's PR #732 adds WorkMotion.Pound (31) for crushing grit and grog. Avatar.Impact fires on each blow at the hands' tool point, low in front of a kneeling player, 1.2 times a second. Each blow now gives a small grey puff and a few stone flecks that hop and settle (AtmosphereImpacts.cs). It is matched by number, `(WorkMotion)31`, so it compiles before #732 lands and works as soon as it does.

## Progress log
- 2026-10-17 11:10: built; the client compiles with 0 errors.
- 2026-10-17 11:12: CI green on 3b4950a4; merged as #733.

## Open questions / blockers
- None.

## Next step
None (done). Once #732 is on main, swap `(WorkMotion)31` for `WorkMotion.Pound`.
