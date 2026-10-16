# survival-6: Survival numbers against D-075 (start low, grow slowly)
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_progress
Branch: claude/survival-numbers (stacked on survival-5 / #75 head 28e9537 plus a ticket commit)
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

## Open questions / blockers
- Waiting on Justin (via PM) for the four picks above. Survival-3's carry-limit question is folded into item 1.

## Next step
When the PM relays Justin's picks, change Carry.BasketBonus (and the others if picked) and add the chill cap in Survival.Chill, update the carry HUD test values if any, push, open a PR.
