# env-39: Painted volumetric clouds in the sky
Lane: Environment (client/scripts/atmosphere handed to this lane by the PM)
Owner: Environment thread (session_014EL1FHDJi92N6gKixNv6Q5)
Status: in_progress
Branch: claude/environment-np240l
PR: (none yet)

## Goal
Justin wants volumetric clouds. Soft painted Ghibli cumulus in the sky with lit tops,
driven by the same cloud_cover and cloud_offset as the ground's cloud shadows so they line
up and drift with the wind; grey and heavy in rain, warm at golden hour; cheap, with a
quality setting. Stills at noon, golden hour and rain through Render.

## How
- Sky shader (Atmosphere.cs): a half-resolution pass (AT_HALF_RES_PASS / HALF_RES_COLOR)
  marches 5 steps (High: 9) through a slab 160 to 230 m up. Each cloud is a dome on a flat
  base, height from the same field as Look.cloud_shadow (0.01 scale, edge mix(0.68, 0.2,
  cover)) plus a finer puff layer, placed over the world from the camera (cloud_cam) so a
  cloud overhead sits over its shadow. Lit in soft painted bands from the dome height and
  how clear it is toward the sun: warm white tops (peach at a low sun), cool lavender-grey
  bases. Rain (rain_heavy) and incoming fronts grey them and weigh them down; storms bruise
  the bases as before. Far clouds fade into the horizon haze.
- The radiance cubemap and the Flat setting keep the old single layer.
- AtmosphereClouds.cs: the setting (graphics/clouds, default Painted; --clouds=0|1|2) and
  the per-frame camera/rain uniforms. Options row "Clouds: Flat / Painted / High" in
  OptionsPage.cs (UX's file, a small hook like the other graphics rows).

## Progress log
- 2026-10-17 16:00: written; not run here (the network blocks the Godot download).
  Cost: half-res, 5 steps x 4 texture reads per sky pixel.

- 2026-10-17 16:15: PM: Justin likes the Stylized Nature Mega Kit look (bright cyan sky,
  painted cumulus, wispy streaks; /mnt/project-files/Stylized_Nature_MegaKitStandard.zip
  Preview_1). Added high wispy streaks stretched along the wind on fair days, whiter
  tops, lavender-grey bases and crisper bands. A brighter cyan sky is a palette change
  (D-154): offered to the PM as options, not built.

- 2026-10-17: sky option 2 (PM/Justin): daytime top and noon horizon moved halfway toward the Stylized Nature kit's cyan; dawn, dusk and night unchanged.

- 2026-10-17 15:55: CI on 4d1f443e green (14/14), https://github.com/Indenex/LifeGame/actions/runs/39045459794; green sha sent to the PM.

- 2026-10-17 16:15: Render's smoke frame at noon, cover 0.45 showed a solid cream sky: any cloud at all along a ray counted as cloud, so slanting rays through the slab nearly always hit one. Clouds are now only the heart of each shadow patch, with thin edges and a shorter march, so a fair sky keeps blue gaps.

- 2026-10-17 17:05: paused by Justin (usage). Findings from local renders (Godot now downloads here; tools/render/setup.sh plus `apt-get install dotnet-sdk-8.0`):
  - Painted clouds never drew: the sky shader lacked `render_mode use_half_res_pass`, so HALF_RES_COLOR was empty. Fixed in d8e3460b, which also keeps the scene fog off the sky (`disable_fog`, which washed the dome to cream) and lays haze only along the horizon (`haze_color` = FogLightColor), plus flatter (40 m) jittered cumulus with thinner cover. Local frames now show cumulus with blue gaps.
  - The noon sky is still pale grey-blue (bluest gap about rgb 176,187,188). With `--grade=0` it is clearly blue (169,196,216); --cosy=0, --soft-glow=0 and --vignette=0 barely change it. So AtmosphereGrade (the faded/cosy colour curve, DOF blur or its fog changes) greys the sky. Not yet narrowed down; it's a look Justin picked (D-313), so any change to it goes to the PM as options.

## Open questions / blockers
- (none)

## Next step
Narrow which part of AtmosphereGrade.Grade() greys the noon sky (local render of the sky shot `--offline --debug-position=0,12 --render-cam=6,1.2,0,4.7 --time-of-day=0.5 --cloud=0.3` with each of: AdjustmentColorCorrection null, DofBlurAmount 0, the FogLightColor/FogAerialPerspective lines skipped), then send the PM options. #843 is held until the sky frame shows blue gaps.
