# claims-16: Client-side audit of the Claims lane (D-182 follow-up)
Lane: Claims, chat and PvP rules
Owner: Claims lane (Sonnet session)
Status: in_review
Branch: claude/claims-16-client-audit
PR: (see below)

## Goal
Check that nothing in `client/scripts/social/` decides a game rule the server does not also decide, and that no player-supplied text reaches other players unfiltered.

## Findings (2026-10-16)
- Every Reducers.* call in client/scripts/social (ClaimPanel, SocialLayer, Fighting, Trading) maps to a server reducer audited in claims-13: reach, ownership, cooldowns and limits are checked on the server. The client only predicts (strike offer, tend prompt, trade ask) and draws; health, grace and marks are read from server rows.
- Gap: control characters (newlines, escape codes, NUL) in chat text and claim names went through unchanged to other players' screens. Fixed in this PR: `Social.StripControl` turns them into spaces; used by `SendChat` and `CleanClaimName`. SocialTest has a check.
- Not mine, for the PM/Integration: `SetName` in Reducers.cs (shared file) trims and length-checks (1 to 24) but still allows control characters. Suggest `name = Social.StripControl(name).Trim();`.

## Progress log
- 2026-10-16: audit done; fix built; SocialTest passes locally on a fresh database; client not built here (no Godot).

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR and send the PM the green sha. Do not push again after the green report.
