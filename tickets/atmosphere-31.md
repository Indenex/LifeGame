# atmosphere-31: 3D, interactive water with reflections
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-31-water
PR: #517 (draft)

## Goal
Justin, verbatim: "the ripple animation is horrible. Can we get 3d water/more interactive water with reflections?" Top priority, coordinated with Environment (water edges). Replace the painted ripple-ring sprites with real stylised 3D water:
- a waving surface;
- reflections of the sky, trees and player, softened;
- the bed seen through the shallows, deepening to dark teal, with a shore fade and foam line;
- an interactive GPU wave simulation driven by waders, swimmers, splashes, rain and animals, so wakes spread, bounce off shores and fade;
- later, floating leaves and stream flow.
Painted colour bands, not photoreal. Measure the cost, add a Graphics setting for reflection quality, and send Render C a before/after clip of wading and floating. Done: Render's clip shown to Justin, he's happy, and the PR is merged.

## Progress log
- 2026-10-17 04:30: first prototype, client builds with 0 errors, not run yet.
  - `WaterLook.cs` (new): the water shader.
    - Near mode: a 96 m mesh at 0.5 m spacing following the player, with a wind-led swell moving its vertices plus the simulated ripples. Far mode: Main's 500 m plane, which gives way inside 44 m.
    - Fine ripples and rain rings in the shading. The bed is refracted from the screen texture, guarded so nothing in front of the water is pulled in.
    - The depth tint falls in three soft painted bands to dark teal. A wavering foam line runs along the bank, with foam on high crests.
    - Screen-space reflections are marched in the shader (16 or 32 steps, softened with a mip blur) and fall back to the sky's top and horizon colours. Fresnel is stylised, and the sun glint is a crisp painted shape.
  - `WaterRipples.cs` (new): the wave simulation.
    - Two 256² HDR SubViewports take turns, covering 32 m at 12.5 cm per texel, stepped at 60 Hz. They follow the player in whole texels, with a shift so the waves stay put in the world.
    - A shore map (48 m at 0.5 m per texel, rebuilt every 6 m) holds the height at zero on land, so waves bounce off the shore.
    - Up to 16 pushes per step: waders, swimmers, splashes, raindrops, and `Atmosphere.DisturbWater` for Animals.
  - `AtmosphereWaterSurface.cs` (new): the near mesh, the ripples, and per-frame globals (sky colours from ApplyLight, sun or moon, ambient, reflection steps). Graphics setting `graphics/water_reflections` 0, 1 or 2, with `--water-reflections=` to override.
  - `AtmosphereWater.cs`: the painted ring sprites are gone. Waders and others now push the simulation; the splash and wet legs stay.
  - `Look.cs`: WaterSurface and WaterNear now come from WaterLook; the old water shader is removed.
  - `OptionsPage.cs`: a "Water reflections: Off/Normal/High" button under Graphics.

## Open questions / blockers
- Render C: before/after clip and GPU cost (`--gpu-bench`, which is on the atmosphere-29 branch only; until then, frame time).
- Environment: their water-edge work. The shore foam and fade now come from the depth behind the water, so the edges need no special mesh.

## Next step
Wait for #517 CI and Render C's before/after clip (requested 04:35). Tune from it, then take it out of draft. Next pieces: floating leaves and stream flow (waiting on Environment's answers).
