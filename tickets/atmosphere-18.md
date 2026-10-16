# atmosphere-18: The forest still looks right on low-end PCs
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-18-low-end
PR: (none yet)

## Goal
Picked under D-145 after atmosphere-13 to 17; sent to the PM as option A of three (A low-end fallback, B tune from the Render lane's shots, C season transitions). The Compatibility renderer (older or low-end PCs, `--rendering-method gl_compatibility`) has no volumetric fog, so the sun shafts in the woods (atmosphere-13) and the soft volumetric mist are missing there. On that renderer only: fold the volumetric mist into the plain fog, lay dawn mist as height fog over the low ground, and stand a few soft additive light columns toward a low sun in the woods. Forward+ unchanged. Done: Render lane shots on Compatibility (woods at dawn and mid-morning, fair; a misty dawn) next to Forward+, and a PR green.

## Progress log
- 2026-10-16 21:45: started from main. New AtmosphereLowEnd.cs: Atmosphere.LowEnd (RenderingServer.GetCurrentRenderingMethod() == "gl_compatibility"); LowEndFog adds mood.MistDensity * 0.6 * (1 + storm) to FogDensity and sets height fog (FogHeight = water + 1.2, FogHeightDensity = 0.12 x dawn mist); five billboard light cards (unshaded, additive, radial gradient, 2.2 to 3.4 m wide, 16 m tall) 6 to 20 m toward the sun, swaying apart slowly, alpha 0.09 x the same shafts value Forward+ uses. Hooked into ApplyLight after the fog. Client builds.

## Open questions / blockers
- Justin's pick among A/B/C (via the PM); working on A meanwhile.

## Next step
Push; render request on Compatibility vs Forward+; PR once Justin picks A (or the PM says go).
