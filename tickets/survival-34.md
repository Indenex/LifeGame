# survival-34: Point back to where you died
Lane: Survival
Owner: Survival thread (session_01BMpUvVCwwzu1SEuBf16iFd)
Status: in_review
Branch: claude/survival-death-pointer
PR: #834

## Goal
After a death you wake up to ~58 m from your dropped things with nothing pointing back. Ported from the unlanded 2026-10-16 prototype (old survival-8): a death-card line ("Your things lie where you fell, to the northwest.", or "close by" under 8 m) and a faint warm shaft of light over the spot (7 m tall, brightest at the ground, gently pulsing) that fades in slowly and fades away once you come within 3 m. Each part has its own switch: `SurvivalClient.DeathCardDirection`, `DeathMarker.Shown`.

## Progress log
- 2026-10-17 15:20: Vitals gains `DiedX`, `DiedZ` (`[Default(0f)]`, additive, private table seen only through my_vitals), set in `Die`. `NeedsHud.ShowDeath` takes the optional line for every wake kind. `client/scripts/survival/DeathMarker.cs`. `--death-preview=SECONDS` fakes a death 13 m northwest for screenshots. Server compiles; the client could not be built here (no Godot/wasm toolchain in this container), CI builds it.

- 2026-10-17 15:15: Justin picked "Line and light" (both), relayed by the PM. #834 marked ready.

## Open questions / blockers
- (none)

## Next step
Wait for CI on #834, send the green sha to the PM; render request (`--death-preview=5`, card plus light shaft) sent to the PM.
