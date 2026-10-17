# bugfix-65: no square shadows from flames, smoke or drips
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-6
PR: (opening)

## Goal
Justin's playtest (`/mnt/project-files/bugreports/2026-10-17-1517/`): a hard square block of shadow on Pebble's shell (shell-shadow.png), and "flickering shadows everywhere" round the lean-to at night (leanto-shadow.png). Cause: particle systems cast shadows by default in Godot 4, and each particle is a square card. The campfire's flames and sparks sit round its shadow-casting OmniLight, so every moving card throws a jumping square shadow over everything the fire lights, which is the flicker. The wet drip on the player, chimney smoke, kiln and pot steam, and the build, gather and sheep puffs do the same in sun or moonlight, which is the square on the shell. Done means no particle system casts a shadow; the fire's light and the shadows of solid things stay as they are.

## Progress log
- 2026-10-17: `CastShadow = Off` on the nine particle systems that lacked it (FireView flames and sparks, WetLook drip, AtmosphereHomes chimney smoke, KilnView, SimmerView, BuildingSystem, Main, Sheep). The other seven already had it. Client only; no local build here, so CI builds it.

## Open questions / blockers
- (none)

## Next step
Wait for CI; send the green sha and a before/after render request (night campfire by the lean-to, wet Pebble in sun) to the PM.
