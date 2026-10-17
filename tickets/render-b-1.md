# render-b-1: UI panels and gameplay stills (Render lane B)
Lane: Render B (UI panels and gameplay stills; Render A does look checks, Render C clips)
Owner: Render B session (session id unknown to this file; started 2026-10-17 from the PM's brief)
Status: in_progress (queue below is still owed)
Branch: claude/render-b-ui (renders in renders/, tooling in tools/render/; never merged to main)
PR: none (renders stay on this branch; only tools/render/ changes would go in a small PR)

## Goal
Take UI and in-game stills requested by lanes via the PM, say plainly what is seen, and send raw GitHub image URLs on this branch to the PM only (never to Justin). Justin's requests via the PM jump the queue.

## How it runs (read this first)
- Setup: `bash tools/render/setup.sh` (also installs xdotool and imagemagick). Software Vulkan, about 0.5 fps, one render at a time (4 cores).
- Panels: `tools/render/render.sh <ticket> <sha> "<client flags>"...` with the game's own `--screenshot-<panel>` flags plus `--offline --render-slowstart=0.05,45 --screenshot-delay=8`; `RENDER_SIZE=1280x720`.
- Scenes with a second player: `tools/render/scene.sh <ticket> <sha> <scene file>` (puppet; steps include give, goto, pos, sh, key, mouse, click, type, snap). `give` adapts to old and new inventory_item columns.
- Key presses are unreliable at 0.5 fps, so panels open from render-tree-only patches in tools/render/patches/: opencraft.sh (RENDER_CRAFT), opentown.sh and seedtown.sh (Town panel; SeedTown/SeedInvite reducers), buildcycle.sh (build tab by /tmp/render-tab), buildtab2.sh (RENDER_TAB), needs.sh (RENDER_NEEDS=food,water,warmth), nolook.sh (first-join picker).
- Godot sometimes hangs after saving its image: scratchpad wd.sh killed any Godot older than 7 minutes or after its PNG exists (not in repo; just `kill` the hung Godot).
- Shoot current origin/main unless the request names an unmerged PR (PM rule). Fetch PR heads with `git fetch origin pull/<n>/head`. Warm/cold pairs: `--cold`.
- Showcase views (chest, sundial, devices): `TICKET=<t> VIEWS="chest --yaw=40;sundial --time-of-day=0.5" tools/render/scenes/model-1-style.sh <sha>`.

## Progress log (latest first; all pushed on claude/render-b-ui)
- 2026-10-17 14:35: wrap-up. House Showcase views on main 36ac39d are in renders/house-show-36ac39d-* (chest x3 yaws, sundial noon and 15:00; devices view was still rendering, not reviewed, NOT sent to the PM).
- Sent to the PM (with what was seen): #489 crafting menu, #494 station panel, frost #459/#597 (3 rounds), #471 races, #477 marks, #535 Town panel, #617 trees, #609/#671/#731/#754/#779/#785/#788 warm paper pairs, ui-39 characters, #565 and #690 haze, #767 golden hour, UX sweep on main (keys/options/controls/menu/use/playermenu/emotes/hotbar/wake/sundial), #809 paper menus pairs.

## Still owed (not yet shot unless noted)
1. House building: chest #759 and straw bed #781 (both merged on main): Showcase chest shots exist in renders/ (not yet reviewed or sent); straw bed needs the Shelter build tile (from inside a claim) plus a placed bed in the world; the sundial Showcase shots exist (unsent) and the in-game arc with a built sundial is not shot; devices view (settling tub, crushing block, tempering trough, #285) not shot.
2. UX: #809 warm re-take of keys and controls at 17c3c7ea; #793 UI size at 2560x1440 (branch claude/ux-ui-sweep2-xlrh2s 6d324690) HUD and characters; #796 bag tooltips (4e41685c); #706 HUD Use prompt and build placement chip on main; ui-37 rest spot prompt (#709); ui-56 world labels (#712); journal, claim panel and storage window (need a puppet scene); Esc menu done.
3. Atmosphere: #608 cosy before/after, #612 glow, #627 snow, atmosphere-29 painterly grid (ef483f6c), all after the PM's order.
4. Modeling: painted before/afters #559, #554, #561, #560, #573, sundial showcase (model-12); Ceramics #616 slip demo; ui-45 hotbar paper buttons; ui-47 wake veil (done in the sweep); ui-48 emote wheel (done in the sweep).
5. Render A's own queue for B (tools/render/queue.b.jobs on claude/render-1-013qe8) was never started.

## Open questions / blockers
- Nothing waiting on Justin from this lane.

## Next step
Fetch origin main, run tools/render/setup.sh if the container is fresh, then do item 1 (review renders/house-show-*, send to the PM and House building session_01DD9p6EhSc5pJ8CjNmDFTrG, then shoot the straw bed).
