# survival-6: Survival numbers against D-075 (start low, grow slowly)
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_review
Branch: claude/survival-numbers (main after #75 merged)
PR: (none yet)

## Goal
Re-check every Survival number that grows with progression against D-075: start low, gains a bit quicker through the first ~15% of progression, then a slow climb, with headroom left for later gear. Send the PM proposed changes as options; once Justin picks, change the constants (shared/Carry.cs, shared/Needs.cs, Survival.cs FireHeat), update the wiki lines that quote them via the Wiki lane, and open a PR.

Out of scope: need drain rates (90/60/16 min, Justin's choice), wetness (D-062), burnt mutton 10% (D-068), shelter warmth values (Building's, shared/Structures.cs; flagged to the PM only).

## Current numbers (what ships on #75)
- Carry: bare hands 20 load, basket +15 (35, +75%). Bricks weigh 3, clay 2.
- Food: berries 12% each, roast mutton 40% (D-039), burnt 10% (D-068). Pinch pot of water 50%, coil pot 100% (D-030).
- Fire: FireHeat 1/30 per s, so a fire warms you from empty to full in about 30 s at full fire warmth.
- Shelter warmth (Building): lean-to 0.3, hut 0.75, house 0.85, closed door 0.9, brick house with fire 1.0.

## Proposed, revised for D-081 (survival is light: nudge, never punish, no death spirals; D-075 is for gear and skills). Sent to PM 2026-10-16; defaults = recommended
1. Carry: A) bare 20 (unchanged), basket +10 = 30, later carriers climb slowly toward ~50 [rec]; B) keep 20/+15; C) bare 25, basket +8 = 33.
2. Food: A) keep berries 12%, roast mutton 40% [rec]; B) berries 15%.
3. Fire: A) keep 30 s [rec]; B) 20 s.
4. New, death-spiral check: cold in freezing rain is ColdDrain x2 (0 C) x1.5 (soaked) = warm to dead in ~5 min away from fire. A) cap the combined chill at x2, so ~8 min worst case [rec]; B) cold can't kill: at empty warmth you shiver and walk slower; C) keep.
Milestone context (PM): M2 "the first evening", a new player reaches a fired pot in 1 to 2 h.

## Progress log
- 2026-10-16: started after D-075; collected current numbers; options sent to PM. No code changed yet.
- 2026-10-16: PM relayed D-081 (light survival); re-cut the options (above) and resent.

- 2026-10-16: D-099 (Justin): cap combined chill at 2x. Done: Needs.MaxChill = 2, applied in Survival.Chill (cold x wet). Standing in water (WetChill 3x) stays outside the cap: you can always step out. Seasons (Atmosphere leads) will feed AirTemperature later; Survival owns how season temperature feeds cold. Opened PR for the cap alone; carry/food/fire picks follow when Justin answers.

- 2026-10-16: D-100: carry 20 bare, basket +10 (pick 1A). D-101: food unchanged (pick 2A). Carry.BasketBonus 15 -> 10 in #124. Fire warmth (pick 3) still with Justin; keep 30 s.

## Open questions / blockers
- Seasons (Atmosphere, session_01KvVF5vpkYEHTKNRpHC18WN): seasonal offset goes into AirTemperature (about +8 C summer, -12 C winter); no Survival change needed thanks to the 2x cap. Snow doesn't wet players for now; if Justin agrees (Atmosphere asks), falling snow wets at half rain's rate via SeasonState.Snow.
- Sent to PM: winter starts for new players. A) 30 min of half-speed cold for new characters [rec], B) a lit campfire at the spawn edge in winter, C) nothing. Nothing ships until Justin picks.
- Fire warmth (pick 3) still with Justin; 1, 2 and 4 decided (D-100, D-101, D-099). Survival-3's carry-limit question is folded into item 1.

## Next step
Wait for CI on the survival-6 PR; green: send the sha to Integration (D-095) and the PM. Then apply Justin's fire pick when the PM relays it.
