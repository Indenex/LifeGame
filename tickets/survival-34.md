# survival-34: Point back to where you died
Lane: Survival
Owner: Survival thread (session_01BMpUvVCwwzu1SEuBf16iFd)
Status: blocked (waits on Justin's pick via the PM; built on the recommended option)
Branch: claude/survival-death-pointer
PR: (not yet; opens once Justin picks)

## Goal
After a death you wake up to ~58 m from your dropped things with nothing pointing back. Ported from the unlanded 2026-10-16 prototype (old survival-8): a death-card line ("Your things lie where you fell, to the northwest.", or "close by" under 8 m) and a faint warm shaft of light over the spot (7 m tall, brightest at the ground, gently pulsing) that fades in slowly and fades away once you come within 3 m. Each part has its own switch: `SurvivalClient.DeathCardDirection`, `DeathMarker.Shown`.

## Progress log
- 2026-10-17 15:20: Vitals gains `DiedX`, `DiedZ` (`[Default(0f)]`, additive, private table seen only through my_vitals), set in `Die`. `NeedsHud.ShowDeath` takes the optional line for every wake kind. `client/scripts/survival/DeathMarker.cs`. `--death-preview=SECONDS` fakes a death 13 m northwest for screenshots. Server compiles; the client could not be built here (no Godot/wasm toolchain in this container), CI builds it.

## Open questions / blockers
- Justin (asked via the PM 2026-10-17): both (recommended), card line only, light only, or neither. Old screenshot: /mnt/project-files/screenshots/17-survival-death-card.png.

## Next step
When Justin answers, flip the switches to match, open the PR, and send a render request (`--death-preview=5`) to the Render lane through the PM.
