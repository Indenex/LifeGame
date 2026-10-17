# release-8: Launcher look: warm paper windows
Lane: Release & Launcher
Owner: Release & Launcher thread
Status: in_review
Branch: claude/release-11
PR: (see below)

## Goal
D-314: both launchers match the game's "Warm paper windows" look: cream paper panel with 24 px corners and soft shadow, brown Nunito text, pastel accents, including the address box, the What's new box and the wipe question.

## Progress log
- 2026-10-17 07:55: palette constants and Nunito (launcher/fonts, raw bytes, exported via include_filter fonts/*) in launcher.gd; screenshots of player, host and the wipe question in renders/release-11.

## Open questions / blockers
- Exact paper colours may need to follow ui-42 (the game's paper frame) when it lands; they are constants at the top of launcher.gd.
- Check in the next release that the exported launcher contains fonts/Nunito.font.bin (else it falls back to the default font).

## Next step
Wait for CI, send the green sha and the renders to the PM.
