# ceramics-3: Clay that explains itself
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session_017sscCMDfHcyeZNqGW22GBE)
Status: in_progress
Branch: claude/clay-ceramics-kfr4kr
PR: #44

## Goal
From Crafting's time-to-pot playtest: drying is the longest wait and a new player can't tell what's happening. Done when:
(1) G by a wet pot or brick says "Drying, about N min left"; beside a fire it adds "(G twice fires it wet)", and away from a fire G says it would dry twice as fast beside one;
(2) a first-time tip says clay dries twice as fast beside a burning fire;
(3) "Feed the fire first" says how many sticks are still needed (a stick is 90 s of fuel);
(4) a first-time tip teaches tending clay with G (clay no longer has a V key; everything is on G since D-025).
Justin sees screenshots of the prompt.

## Progress log
- 2026-10-16 00:15: from main 7de231e. ClayRules.SecondsUntilDry / DryWait / SticksForFiring; PotView.SecondsUntilDry; CeramicsView TendOffer shows the wait for wet clay; server fuel message counts sticks. Tips shown once per install via the HUD note, queued 4.5 s apart (user://ceramics_tips.cfg), until UX adds a free-text Hints.Tip (asked UX). ClayCheck --kiln now compares fired qualities per pot (two pots can differ by a point). Server, client, ClayCheck build.
- 2026-10-16 00:30: screenshots clay-06 (kiln built in game) and clay-07 (drying prompt) sent to PM; PR #44 opened.

## Open questions / blockers
- Asked UX for Hints.Tip(id, text); switch CeramicsView.Tip to it when it lands.
- Once UX #38 is on main, pass Target on the clay and kiln G offers.

## Next step
Wait for CI on #44, send the PM the green sha; then tell UX ceramics-3 landed (they drop tend_clay from Controls).