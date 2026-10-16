# ui-5: Esc menu music slider and key list follow-ups
Lane: UX and UI
Owner: UX and UI thread (session_01NAW5B3eCSw3JtCiFrZeSNz)
Status: blocked
Branch: claude/ux-ui-xlrh2s
PR: (none yet)

## Goal
Small follow-ups that wait on other lanes landing on main:
- Esc menu gets a Music slider and shows whether music is on, reading Atmosphere's `atmosphere.Sound.MusicVolume` (0..1, eased, saved in user://sound.cfg) and `MusicOn` (PR #32).
- F1 key list gets "Shift G: bar or unbar a hut door" once Building's doors (building-3) are on main.
- The tend_clay V row leaves Controls/F1 once Ceramics' ceramics-3 (all clay on G) is on main.
Done when each is on main and Justin has a screenshot of the menu with the music slider.

## Progress log
- 2026-10-16 00:25: opened after ui-4 (#38) merged at ef1c26b. Told Survival, Claims, Ceramics and Building that Panels.Open, Hints.Done/Tip and UseOffer.Target are on main.

## Open questions / blockers
- Waiting on Atmosphere #32 (PM: held for Justin's OK on the firelight look), building-3 and ceramics-3.

## Next step
When any of #32, building-3 or ceramics-3 is on main, merge main into claude/ux-ui-xlrh2s and do that item (PauseMenu.cs for music, Controls.cs rows for keys).
