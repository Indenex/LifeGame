# painted-10: Soft rim light
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/painted-10-rim
PR: (opening)

## Goal
D-313, Justin's pick (idea #1): a thin warm glow on silhouettes facing away from the light. It applies to characters, animals and props, through the shared painted shader, with a small strength constant.

## Progress log
- 2026-10-17 08:05: `Painted.Rim` is a light() snippet, spliced after the stepped light in Painted's shader (so props, rocks, bushes, animals, structures and items get it) and into ChibiLook (characters).
  - It applies to directional light only. It covers the edges where N·V is low on the side facing away from the sun, and is strongest when the shape is backlit.
  - Its colour is the sun's own LIGHT_COLOR times a warm tint, so it follows Atmosphere's light through the day.
  - Trees and Environment's ground don't get it (`Painted.Light` is unchanged).
  - `Painted.RimStrength` = 0.5 is the one constant to raise.
- The constant is printed as a GLSL float ("0.0###"). An integer literal broke the shader in a test with strength 0.
- Render: renders/painted-10-rim-off-on.png, Showcase `roster` at 0.72 from two sides, each pair rim off then on.

## Open questions / blockers
- Atmosphere: whether it wants its own rim tint per time of day. Currently the rim follows LIGHT_COLOR.

## Next step
Open the PR; ask Atmosphere about the rim colour; send a Render request for a backlit dusk still.
