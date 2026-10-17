# anim-10: Chibi wiring (nameplate height, pose check on Bramble and Pebble)
Lane: Animation
Owner: Animation session (Opus), session_019roNRWQBPw4ozNLDhzseig
Status: done
Branch: claude/anim-10
PR: (to open)

## Goal
PM 03:51: once Modeling switches PlayerModel to ChibiFigure (model-6, Bramble and Pebble, 1.35 m), everything Animation owns fits the smaller body. This covers the nameplate height (model-6 plan item 4) and a check of every pose, with Render clips. anim-6 already made hand-to-mouth reaches and hip drops body-relative, and the rig re-measures on SetLook.

## Progress log
- 2026-10-17 04:15: #506 (anim-8, exact tool from craft_work.Tool) and #508 (anim-9, idle gestures) merged. Nameplate height now comes from the top of the Body mesh plus 0.3 m (2.05 m on the adult as before, about 1.65 m on a chibi), placed at build and on SetLook. Client builds. PlayerModel isn't switched to ChibiFigure on main yet (claude/model-6-chibi, 163ac30).

- 2026-10-17 07:30: merged to main; marked done on anim-12.

## Open questions / blockers
- Waiting on Modeling to wire ChibiFigure into PlayerModel.

## Next step
None: merged. Follow-ups (clip fixes) go on later anim tickets.
