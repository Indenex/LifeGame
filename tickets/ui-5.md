# ui-5: Esc menu music slider and key list follow-ups
Lane: UX and UI
Owner: UX and UI thread (session_01NAW5B3eCSw3JtCiFrZeSNz)
Status: blocked
Branch: claude/ux-ui-xlrh2s
PR: #46 (music slider and Shift G row)

## Goal
Small follow-ups that wait on other lanes landing on main:
- Esc menu gets a Music slider and shows whether music is on, reading Atmosphere's `atmosphere.Sound.MusicVolume` (0..1, eased, saved in user://sound.cfg) and `MusicOn` (PR #32).
- F1 key list gets "Shift G: bar or unbar a hut door" once Building's doors (building-3) are on main.
- The tend_clay V row leaves Controls/F1 once Ceramics' ceramics-3 (all clay on G) is on main.
Done when each is on main and Justin has a screenshot of the menu with the music slider.

## Progress log
- 2026-10-16 00:25: opened after ui-4 (#38) merged at ef1c26b. Told Survival, Claims, Ceramics and Building that Panels.Open, Hints.Done/Tip and UseOffer.Target are on main.
- 2026-10-16 04:50: music slider, Music on/off and the Shift G row landed via #46 merged at 63dda46 (CI run https://github.com/Indenex/LifeGame/actions/runs/38851687321 green on 9234fea). Only the V row is left.

## Open questions / blockers
- Waiting on ceramics-3 (all clay on G) for the V row, and on survival-4 for the KEYMAP forage_facing stopgap note.

## Next step
When ceramics-3 is on main, drop the tend_clay row from client/scripts/ui/Controls.cs; when survival-4 is, drop forage_facing from KEYMAP's priorities. Then open a PR.
