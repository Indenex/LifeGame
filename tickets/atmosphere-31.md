# atmosphere-31: 3D, interactive water with reflections
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_review
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

- 2026-10-17 04:45: floating leaves in the shader. A drifting cell grid of pointed ovals near the banks (fading by depth over 0.5 to 4 m): many and turned in autumn, a few green in summer, none when it freezes. They sit on top of the reflections. Environment (env-21, claude/environment-water-edges) keeps its damp shore band and leaves foam and edge alpha to the water; I told them the mean stays at WaterLevel.

- 2026-10-17 05:40: stream flow. A 64 m flow map round the player, 1 m per texel, rebuilt every 8 m: direction is along the stream line (perpendicular to the gradient of Land.StreamDistance), downstream is +z, and strength fades from 4 to 11 m off the line. The shader slides the fine ripples, floating leaves and pale foam streaks downstream with a two-phase flow cycle, and running water is a little rougher. Environment confirmed: streams every ~300 m, the first ~66 m west of spawn (x ≈ -48…-41 at z=10); wet sand stays theirs.
- 2026-10-17 07:40: Render C's clip approved by the PM. Fixed the startup error (WaterLook used the editor-only GlobalShaderParameterGetList); merged main; the client builds with 0 errors. Out of draft.

## Open questions / blockers
- Render C: before/after clip and GPU cost (`--gpu-bench`, which is on the atmosphere-29 branch only; until then, frame time).
- Environment: their water-edge work. The shore foam and fade now come from the depth behind the water, so the edges need no special mesh.

## Next step
Wait for CI on #517, send the green sha to the PM.