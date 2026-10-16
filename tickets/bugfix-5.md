# bugfix-5: Fresh-world playtest, fix small cross-lane issues
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: done
Branch: claude/bugfix-lyle6o
PR: #36

## Goal
Play a fresh world on main with the real client and a bot (walk, gather, fire, cook, dig, clay, build, journal, sheep, every G prompt). Fix small cross-lane issues; report the rest to the PM with screenshots in /mnt/project-files/screenshots/bugfix-05-*.png.

## Progress log
- 2026-10-15: playtested main 04a9b12 under Xvfb. Fixed: crafting panel scrolls instead of running off screen, Esc closes it, tool needs read in plain words (CraftingUi.cs); Journal and Crafting panels on CanvasLayer 3 above the HUD (Journal.cs, CraftingUi.cs). Findings list sent to the PM; PM routed 3-12 to owners (Survival G/R bypass Use key, Building hold H, Ceramics V, no raw mutton source, grass pop under blueprints, canopy fade blob, inventory vs rings, prompt placement, late hints, claim text vs "J to close").
- 2026-10-15 23:40: #25 merged; branch restarted from main 295f672 with the fixes on top (bow drill tool name added after merging crafting-3). Client builds locally.

## Open questions / blockers
- None.

## Next step
None; merged.
