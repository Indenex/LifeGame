# ceramics-5: Clay-path playtest fixes
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session_017sscCMDfHcyeZNqGW22GBE)
Status: in_progress
Branch: local `ceramics-5` (main merged in) until #44 merges, then claude/clay-ceramics-kfr4kr
PR: (none yet)

## Goal
The PM's playtest-3 findings (screenshots /mnt/project-files/screenshots/playtest3-*):
1. The kiln burns the longest-burning fuel first (sticks before fiber and tinder), like feeding a fire.
2. Fresh clay is easy to spot: a soft warm rim light on drying pieces within 8 m (a MaterialOverlay, so the wet-to-pale color still reads), brighter on the one G would tend. A ground disc was tried first and was hidden by grass.
3. A pit-fired pot says "Your pinch pot is fired, and whole. G picks it up." (the kiln note says the same), and G picks up fired pots, filled pots, sherds and dry bricks on the ground (Use handler ceramics.pick_up, priority 10). News text carries "{use}", which the client fills in with the player's Use key.
4. The pinch-pot box: already low on screen since ui-7 (#46) put Hud.PlaceLow in the minigame base. Checked with --ceramics-demo=shape; nothing to change.

## Progress log
- 2026-10-16 05:00: all four done locally on `ceramics-5`; server, client and ClayCheck build.

- 2026-10-16 05:07: ClayCheck pit chain passed on scratch db c5check1; news reads "Your pinch pot is fired, and whole. {use} picks it up."
- 2026-10-16 05:20: rim glow instead of ground disc; screenshots clay-11-fresh-clay-rim-glow.png, clay-12-g-picks-up-sherd.png.

## Open questions / blockers
- #44 must merge first (same branch).
- Bug fixes #68 (bugfix-13) and UX #73 change CeramicsView.cs and ClayItemLook.cs; merge main after they land and keep their Targets and the twice-registered TendOffer (ceramics.fire_pot 22, ceramics.tend 12).

## Next step
Once #44 merges, merge main into claude/clay-ceramics-kfr4kr, cherry-pick the ceramics-5 commit, push, open the PR.
