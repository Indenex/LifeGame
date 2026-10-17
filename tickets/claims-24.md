# claims-24: A town ban is per character (tested)
Lane: Claims
Owner: Claims thread
Status: in_review
Branch: claude/claims-24-ban-per-character
PR: (see PR list)

## Goal
D-323: a Marshal's ban names one character, not the login behind it. The code already did this (a ban stores the banned `Player.Identity`, which is the character key, and the rights check compares it with `Me(ctx)`); this proves it with two characters.

## Progress log
- 2026-10-17: SocialTest (town section): A makes a second character, steps back into the first, the Marshal bans the second; the ban row names the second character's key (not the login, not the first character), and A's first character still builds on B's land (no "claimed" refusal); the Marshal lifts the ban. Needs `my_characters` in the Tester's subscription. Passes locally on a fresh database. No server change.
- Note for later tests: character names are letters only (no digits).

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR and send the green sha to the PM.
