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

## Open questions / blockers
- (none)

## Next step
Wait for the PR's CI (water and clouds together); send the PM the green sha; Render after shots requested.
