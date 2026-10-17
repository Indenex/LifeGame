# bugfix-46: SocialTest "stuck on leg 2/2 to (38.0, 33.4)" flake
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-46
PR: (see PR list)

## Goal
CI "Load, spawn and social checks" failed on #541 with `A stuck on leg 2/2 to (38.0, 33.4): bot (38.3, 31.4), server (38.3, 31.4)`. Root cause: after B lays out a lean-to at (hx, hz), SocialTest sent A to `(hx - 2, hz)` to dismantle it. A lean-to's west side wall stands about 2 m from its middle, so that point is on the wall; which route the bot planned (a 4-leg detour round the wall, or a 2-leg straight line that the wall then blocks) depended on where A's previous walk had ended within half a metre. Reproduced locally (1 of 2 runs failed, same coordinates). Fix: A walks to the open front of the lean-to (1.2 m past its front edge, inside the 2.2 m work reach), which is always reachable.

## Progress log
- 2026-10-17: reproduced; fixed; 3 of 3 runs pass locally afterwards (the earlier two runs were 1 pass and 1 fail).

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, send the green sha to the PM.
