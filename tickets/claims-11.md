# claims-11: Stash log
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session session_013ECn821EbTpbj4h7d3PepH)
Status: in_review
Branch: claude/claims-11-stash-log (stacked on claude/claims-10-voice; merge after claims-10)
PR: https://github.com/Indenex/LifeGame/pull/196

## Goal
A group can trust its shared stash (PM, D-145).
- Under the stash in the claim panel, owners and members see the last five comings and goings, newest first: "Tam took 2 sticks · 4 min ago", "You left a stone · just now".
- Quick repeats (same person, item and direction within a minute) fold into one line.
- The server keeps 20 entries per claim, and they go when the marker falls. Outsiders never see them.

## Progress log
- 2026-10-16 19:56: handoff (D-159: the lane moves to a Sonnet session). PR open; CI run https://github.com/Indenex/LifeGame/actions/runs/38957011366 in progress at handoff (both client jobs green, server job running). Head is on claude/claims-11-stash-log. This copy of the ticket is updated on claude/claims-12-fences; the PR branch's copy says "PR: (not yet)", which was left alone to avoid restarting CI.
- 2026-10-16 19:55: built.
  - Server: `server/spacetimedb/StashLog.cs` (the ClaimStashLog table, the MyClaimStashLog view and LogStash with folding). StashItem and TakeFromStash log; RemoveClaim clears.
  - Shared: `Social.StashLogKeep`, `StashLogFoldMicros` and `StashLine`.
  - Client: the "Recent" lines in ClaimPanel; subscribed to my_claim_stash_log.
  - SocialTest: 3 new checks.
  - Local build passes; all social checks pass.
  - Only a new table, so no wipe.

## Open questions / blockers
- (none)

## Next step
Watch CI on PR #196 (head 7a7cf3e; run linked above). It merges after #191. Once #191 is merged, merge main into claude/claims-11-stash-log, push, and send the PM the green head sha. Then send a render request: the claim panel stash with its "Recent" lines.
