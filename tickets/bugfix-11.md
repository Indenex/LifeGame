# bugfix-11: Nothing shows over the death card
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: in_review
Branch: claude/bugfix-lyle6o
PR: (opening)

## Goal
Found in the PM's playtest pass on main 896e9bd. When you die, the screen fades to a calm black card ("You froze to death. You wake at the edge of the wilderness with nothing."), but the Use prompt for wherever you wake (for example "G Pick berries") and the land chip ("Wild land · fighting allowed") stayed on top of it. Done means both step aside while the card is up and come back as it fades: NeedsHud.Mourning is true while the card's fade is visible, and Main adds it to the Use key's and herd's Blocked checks and to SocialLayer.HideChip.

Screenshots: /mnt/project-files/screenshots/bugfix-11-before-prompt-over-death.png and bugfix-11-after-clean-death-card.png.

## Progress log
- 2026-10-16 03:50: found while playtesting (Forward+ on llvmpipe, Xvfb); fixed; death card verified clean in the client by setting warmth to 0 via SQL.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR and send the PM the green sha.
