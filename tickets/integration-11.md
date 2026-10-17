# integration-11: Hold the key to repeat
Lane: Integration
Owner: Integration thread
Status: in_review
Branch: claude/integration-hold-use
PR: (see GitHub)

## Goal
Justin: "When harvesting things, digging etc let me hold the key down to repeat the action instead of clicking it constantly." Hold E (Use) or F (Dig) and the action repeats: a tap still does one; holding repeats after 0.4 s, each time the last action is done and its interval has passed. Timings unchanged; the server still checks every action (D-182).

## What was built (client only)
- `ui/HoldRepeat.cs`: the shared hold logic (delay, interval, busy, stop on a reducer error, stop after walking 1.5 m).
- `UseOffer.Again` (seconds): marks a repeatable Use offer. `Use.Busy` (set by Main to `CraftingUi.Working`, a craft job or minigame) makes a held key wait for the job; `Use.Failed()` (called from Main's reducer-error hook) stops the hold, which covers target gone or used up, bag full, tool or need missing.
- `UseKey`: repeats only the same offer (same prompt) it started with.
- Offers marked repeatable: chop (Again 0.3, waits for the craft job), pick berries, drink, shear, take eggs, butcher, take leather, fill pots with water. Dig (F) holds in Main with the dig cooldown. Picking things up off the ground with E (no other offer) holds with a 0.5 s interval.
- Controls help and hints, KEYMAP.md updated.

## Not done / follow-ups
- Crafts started from the make panel (knap, cordage and so on) are not on the Use key; holding the craft button is a UX decision (send as options).
- HUD cue (UX): a small "holding" ring; none yet.
- Animation: looping motions blending across repeats.
- Needs a Render clip (hold E at a berry bush, hold F, hold E at a tree).

## Progress log
- 2026-10-17: built, client builds. Not yet run in the game.

## Next step
CI green, send the PM the sha and a Render clip request.
