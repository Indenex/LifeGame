# crafting-2: Shared Use key (G)
Lane: Crafting
Owner: Crafting thread
Status: in_progress
Branch: crafting-2 (stacked on crafting-core / PR #4)
PR: (not yet)

## Goal
One Use key, G, per D-025 (KEYMAP.md on the PM branch). Pressing G does the obvious thing for whatever is nearest in front of you, and a prompt above the inventory ("G  Feed the fire (stick)") says what that will be. Lanes plug in through `client/scripts/use/UseTargets.cs`: `UseTargets.Register(IUsable)`, or `new Usable(priority, (pos, heading) => UseTarget?)`. Feed fire and pick berries move onto it; eat moves to X.

## Progress log
- 2026-10-15: registry, HUD prompt, feed fire and pick berries on G, eat on X. Client builds. Building's "hold H to add" and Survival's R drink stay until those lanes move them.

## Open questions / blockers
- Waits on PR #4 merging; then rebase onto main (merge main in) and open the PR.

## Next step
After #4 merges: merge main into crafting-2, build, run the client against a local server for a screenshot of the G prompt by a fire, open the PR.
