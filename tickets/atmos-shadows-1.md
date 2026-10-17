# atmos-shadows-1: steadier, crisper sun shadows
Lane: Atmosphere (lighting), done by the Unity spike worker for the PM; Atmosphere owns the files
Owner: session_017Weex8fujB3VFsrpg84jEP (Unity spike worker)
Status: in_progress
Branch: local `atmos-shadows-1` off main 27fffbd3; push target waiting on the PM (this session may only push claude/unity-spike)
PR: none yet

## Goal
Bring the steadier, crisper shadows Unity URP would give (unity-1 write-up) to the Godot client:
cascades fitted to the follow camera, blended splits, no shimmer while walking or as the sun
moves, bias tuned so painted surfaces neither acne nor float, atlas and soft filter per quality
level, and an Options > Shadows setting (Low/Medium/High). Frame time and draw calls measured
before and after; Render shows noon and dusk, walking, before and after.

## Progress log
- 2026-10-17 06:50: started from the PM's message. Findings: Godot 4.6 already texel-snaps
  directional cascades (renderer_scene_cull.cpp), so camera motion alone is stable; the crawl
  comes from Atmosphere turning the sun every frame (360 deg per 90 min day). Godot's default
  splits (0.1/0.2/0.5 of 60 m) put the player (16 m from the camera) in the third cascade.
  Godot 4 has no contact shadows (removed in 4.0), so none.
- 2026-10-17 07:05: local commit 3577a366: client/scripts/atmosphere/SunShadows.cs (fitted
  splits refit when zoom settles, blend splits, bias 0.03 / normal 1.2, sun direction stepped
  0.1 deg, tiers Low 2048/2 splits/40 m/soft low, Medium 4096/4/60/soft medium, High
  8192/4/70/soft high); hooks: Atmosphere.cs (adds the node, steps the sun), OptionsPage.cs
  (Shadows button, like Light rays). Client builds.
- 2026-10-17 07:10: measuring main vs this: 40 frames at --fixed-fps 30, noon and 0.725,
  /root/perf/out-<sha>-<t> (frame PNGs + stats.json).

- 2026-10-17 08:05: measured (fixed 30 fps game step, 40 frames, offline spawn, 1280x720,
  software Vulkan/llvmpipe, so frame ms are relative only; GPU timers read 0 there).
  "flicker" = % of pixels changing >6/255 between consecutive frames with the player still
  (grass sway is part of it in every run).

  | run | draw calls | primitives | frame ms (sw) | flicker |
  |---|---|---|---|---|
  | main, noon | 701 | 1.96 M | 2323 | 2.23% |
  | Low, noon | 501 | 1.65 M | 2298 | 1.94% |
  | Medium (trimmed, default), noon | 718 | 2.42 M | 2783 | 1.71% |
  | High, noon | 1182 | 3.30 M | 3493 | 1.78% |
  | main, dusk 0.725 | 1540 | 4.16 M | 3484 | 0.39% |
  | Medium, dusk | 1819 | 4.97 M | 4102 | 0.39% |
  | High, dusk | 2401 | 6.04 M | 5333 | 0.24% |

  First Medium (60 m, wide fit) cost 1010 draw calls at noon; trimmed to 50 m with the near
  cascades tight (6b624d5c). Default is now Medium; High is the opt-in crisp mode. Crops of
  noon (main / Medium / High): tree shadow edges visibly crisper on Medium and High.

## Open questions / blockers
- Push target: PM to confirm claude/atmos-shadows-1 (or another route) before push and PR.

## Next step
Push once the PM confirms the branch, wait for CI, send Render the before/after request (noon and dusk, walking), send the PM the green sha.
