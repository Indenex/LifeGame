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

## Proposed (sent to PM 2026-10-16; defaults = recommended)
1. Carry: A) bare 15, basket +6 = 21 (+40%), room for later carriers to reach ~40 [rec]; B) bare 20, basket +8 = 28; C) keep 20/+15.
2. Food: A) keep berries 12%, roast mutton 40% -> 30%, room for later pot-cooked meals ~50% [rec]; B) keep as is; C) berries 8%, mutton 25%.
3. Fire: A) basic campfire warms you in 60 s instead of 30, room for better hearths later [rec]; B) keep 30 s; C) 90 s.

## Progress log
- 2026-10-16: started after D-075; collected current numbers; options sent to PM. No code changed yet.

## Open questions / blockers
- Waiting on Justin (via PM) for the three picks above. Survival-3's carry-limit question is folded into item 1.

## Next step
When the PM relays Justin's picks, change Carry.BaseLimit/BasketBonus, Needs.RoastMuttonFood and Survival.FireHeat accordingly, update the carry HUD test values if any, push, open a PR.
