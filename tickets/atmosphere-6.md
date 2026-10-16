# atmosphere-6: Trees topple and rocks sink when ground is cleared
Lane: World atmosphere and sound
Owner: Atmosphere thread
Status: in_progress
Branch: local atmosphere-6-wip (stacked on atmosphere-5-wip); will push to claude/atmosphere-sound-hlqfke after atmosphere-5 merges
PR: (after atmosphere-5)

## Goal
When a cell is cleared (a blueprint placed, ground dug), its trees and rocks no longer vanish at once (D-036, immersion). Trees lean over in a direction seeded per tree from its cell (D-056, the same for every player), slow then fast, and settle into the ground over 2.4 s; rocks sink over 1.6 s; anything on a cell that comes back grows in. Justin sees a tree fall instead of popping.

## Progress log
- 2026-10-16 00:45: Terrain keeps trees and rocks on freshly cleared cells with MultiMesh custom data (start, +1/-1, rock flag); the painted shader topples, sinks or grows them from world_seconds. Fall direction is away from the camera ±26°. `--debug-clear-radius` added. Renders: screenshots/atmosphere-17-fall-{before,early,mid,done}.png (Forward+).
- 2026-10-16 01:05: Justin chose a random fall direction (D-056); direction now hashed from the tree's cell, so all clients agree.

## Open questions / blockers
- (none)

## Next step
After atmosphere-5 merges: merge main into atmosphere-6-wip, build, push to the assigned branch, open the PR.
