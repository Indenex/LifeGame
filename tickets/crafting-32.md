# crafting-32: A cooldown refusal does not end a held dig
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-holdfix
PR: (see below)

## Goal
Render C's clip of #495 (hold F to dig) showed one dig, "Still working" for about 3 s, then no second dig. The server refuses a dig inside its cooldown with "Still working"; the client treated every reducer error as "stop this hold until the key is pressed again" (`Use.Failed` -> `HoldRepeat`), so one repeat that landed a hair early on a laggy link ended the whole hold. `Main.cs` now ignores that one message for `Use.Failed`; the hold tries again after the next interval (the note still shows). The recording may also be distorted by its fixed frame steps (Render C's bag-count check will settle that); this fix removes the fragile path either way.

## Progress log
- 2026-10-17: found the path, one-line fix, client builds.

## Next step
None: merged (#670). Later sections below are history.
