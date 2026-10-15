# bugfix-5: Fresh-world playtest, fix small cross-lane issues
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: in_progress
Branch: claude/bugfix-lyle6o (commits built locally on bugfix-5-local; pushed after #25 merges)
PR: (not opened yet)

## Goal
Play a fresh world on main with the real client and a bot (walk, gather, fire, cook, dig, clay, build, journal, sheep, every G prompt). Fix small cross-lane issues; report the rest to the PM with screenshots in /mnt/project-files/screenshots/bugfix-05-*.png.

## Progress log
- 2026-10-15: playtested main 04a9b12 under Xvfb. Fixed: crafting panel scrolls instead of running off screen, Esc closes it, tool needs read in plain words (CraftingUi.cs); Journal and Crafting panels on CanvasLayer 3 above the HUD (Journal.cs, CraftingUi.cs). Findings list sent to the PM (Survival G/R bypass Use key, Building hold H, Ceramics V, no raw mutton source, grass pop under blueprints, canopy fade blob, inventory vs rings, prompt placement, late hints, claim text vs "J to close").

## Open questions / blockers
- None. Waiting on #25 to merge so claude/bugfix-lyle6o can be restarted from main.

## Next step
After #25 merges: reset claude/bugfix-lyle6o to origin/main, cherry-pick the bugfix-5 commits, run tools/gen-bindings.sh, build the client, push, open the PR.
