# anim-24: Pounding grit and grog
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: in_review
Branch: claude/anim-24 (from main)
PR: #732

## Goal
Crushing stone into grit and potsherds into grog (clay.grit, clay.grog) now looks like pounding instead of kneading. Kneeling, the player raises a stone in both hands to the chest and brings it down onto the pile in front, bending into each blow, 1.2 times a second. Each blow fires Avatar.Impact (WorkMotion.Pound, near the end of each downstroke), so Atmosphere and Audio can add dust and a thud. This is the first step of option (c), per-tool polish.

## Design
- `WorkMotion.Pound` is appended (31).
- `WorkMotions.For` maps clay.grit and clay.grog to it. They have no station or Motion set on their Def.
- The pose and `PoundRate` = 1.2 are in WorkMotions; WorkImpact gives `(PoundRate, 0.99)`.
- I surveyed the other processes. Sewing happens at stations (standing station pose), so no separate sew motion was added.
- Also marks anim-23 done.

## Progress log
- 2026-10-17 11:05: built; server and client build.
- 2026-10-17 11:15: moved the grit/grog motion onto their ProcessDefs (`Motion = Pound`, D-206) instead of a special case in `WorkMotions.For`; `hide.scrape_deer` (a hand craft) now scrapes instead of falling back to Gather. Both build.

## Open questions / blockers
- (none)

## Next step
Wait for CI and send the PM the green sha. Tell Atmosphere and Audio about WorkMotion.Pound impacts.
