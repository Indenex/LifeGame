# bugfix-66: characters stay matte, never blinding
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-7
PR: (opening)

## Goal
Justin's playtest: "At certain angles the light washes out your character and is way too reflective and blinding." There were two causes. (1) The wet overlay (`WetLook`, survival-5) used a glossy GGX highlight (roughness 0.35, specular 0.35), which caught the sun or a fire at some angles and, with glow, blew out the figure. It's now a broad, faint sheen with a capped strength, rough enough that sky reflections stay dull. (2) The chibi body and shell shaders drew every fire and lamp at full strength across its whole range, because attenuation only picked the band. They now fade with reach, as the painted props do. Done means characters read matte and painted from every angle, wet or dry, by day or by a fire.

## Progress log
- 2026-10-17: `WetLook.cs` gets a custom `light()` and is matte (roughness 0.7, specular 0.1). `ChibiLook.cs` and `ChibiShell.cs` scale positional lights by `reach`. No local build, so CI builds it.

## Open questions / blockers
- (none)

## Next step
Wait for CI; send the green sha and a render request (wet Pebble against a low sun, and beside a campfire, turned through 360 degrees) to the PM.
