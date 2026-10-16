# survival-16: TrySpendStamina for jumping
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_review
Branch: claude/survival-spend-stamina
PR: (to open)

## Goal
Bug fixes lane (bugfix-25, Justin's Space jump, default 8% of the bar per jump, not while winded) asked for `bool TrySpendStamina(float fraction)` on SurvivalClient: false and nothing spent if winded or the bar holds less than the cost; resets the refill delay. Bugfix adds Stamina.JumpCost and the call.

## Progress log
- 2026-10-16: added; client builds locally.

## Open questions / blockers
- (none)

## Next step
Open PR, wait for CI, send green sha to the PM and tell the Bug fixes lane (session_01AeuMsF8LfzDsWW8jUh1Fkp) when it merges.
