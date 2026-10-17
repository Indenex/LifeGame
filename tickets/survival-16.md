# survival-16: TrySpendStamina for jumping
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: done
Branch: claude/survival-spend-stamina
PR: merged, first commit 916b7919

## Goal
Bug fixes lane (bugfix-25, Justin's Space jump, default 8% of the bar per jump, not while winded) asked for `bool TrySpendStamina(float fraction)` on SurvivalClient: false and nothing spent if winded or the bar holds less than the cost; resets the refill delay. Bugfix adds Stamina.JumpCost and the call.

## Progress log
- 2026-10-16: added; client builds locally.

## Open questions / blockers
- (none)

## Next step
Done and merged; nothing further on this ticket.
