# crafting-10: Crafting panel stays open after a craft
Lane: Crafting
Owner: Crafting thread
Status: in_review
Branch: crafting-panel-stays
PR: #72

## Goal
Playtest finding: the panel closed after every craft, so cleaning 6 clay meant pressing C then 1, six times. Now the panel stays open, and pressing the same number repeats the recipe once your hands are free. It steps aside only while a minigame is played and comes back after it. C and Esc close it as before.

## Progress log
- 2026-10-16 05:00: `Begin` no longer hides the panel for hands-off processes. A minigame hides it and `EndMinigame` reopens it if it was open. G-started processes, such as chopping, never open it. Client builds.

- 2026-10-16 05:10: (branch crafting-minigame-sound, stacked on #72) added a `MinigameSound(def, playing)` partial hook. It fires when any minigame opens and when it closes, Ceramics' Shape and Coil included, so Atmosphere can play `Sound.Working` during the hands-on part (D-036). Atmosphere implements it in its own file.

## Open questions / blockers
- UX may register the panel with `Panels` in #46; this change only touches `Begin`/`EndMinigame`, so the two should merge cleanly.

## Next step
Wait for CI; the PM merges.
