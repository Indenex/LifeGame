# anim-25: no hand craft falls back to the gather pose
Lane: Animation
Owner: Animation thread (session_019roNRWQBPw4ozNLDhzseig)
Status: done
Branch: claude/anim-25
PR: #739

## Goal
Per-tool polish (option c). Every hand craft shows a fitting motion instead of the generic gather pose. The two that still fell back are fixed as data on their ProcessDefs (D-206). Shredding tinder (`gather.tinder`) now rolls the fibre between the palms (Twist), and sewing a waterskin (`hide.waterskin`) holds the hide and draws the cord through (Wrap). Justin sees the player's hands working the material rather than reaching for the ground.

## Progress log
- 2026-10-17 11:25: branch off main; set `Motion` on both Defs.
- 2026-10-17 11:30: client and server build; pushed and opened the PR.
- 2026-10-17 11:32: CI green at 4c3de91d; merged (#739).

## Open questions / blockers
- (none)

## Next step
Done: merged in #739.
