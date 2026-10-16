# claims-12: A lived-in marker
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session session_013ECn821EbTpbj4h7d3PepH)
Status: in_progress
Branch: claude/claims-12-lived-marker (stacked on claude/claims-11-stash-log; merge after #196)
PR: (not yet)

## Goal
M3, "a place of your own". A claim marker shows how long the place has been kept:
- after 1 day standing, a cord is wound round the post;
- after 3 days, more stones gather in a ring at its foot;
- after 7 days, three notches are carved in the post and a small stone charm hangs from the crossbar.

The details lean with the post as it weathers, and they go when the marker falls. Slow on purpose (D-075). Purely visual (D-081).

## Progress log
- 2026-10-16 20:10: built on the default while the PM decides between the claims-12 options (1 lived-in marker [recommended], 2 visitors noticed, 3 claim fences).
  - Server: `server/spacetimedb/ClaimAge.cs` (a public claim_age table, ClaimId → RaisedAt), filled when a claim is raised and on tending. Claims raised before this get their age from their first tend after the update. RemoveClaim clears it.
  - Shared: `Social.LivedIn(raisedAt, now)` returns 0 to 3, with thresholds `LivedInDays` {1, 3, 7}.
  - Client: `ClaimMarker.LivedIn.cs` builds the cord, stones, notches and charm, re-checking every 30 s.
  - SocialTest: 3 new checks.
  - Local build passes; all social checks pass.
  - Only a new table, so no wipe.

## Open questions / blockers
- Sent to PM 2026-10-16: the claims-12 options above.

## Next step
After #196 merges and the PM confirms option 1: merge main in, open the PR, and send a render request (a marker at each level; the Render lane needs claim_age rows backdated with spacetime sql).
