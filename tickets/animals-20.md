# animals-20: Hen roosting (coop is House building's)
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-roost2-5gkntn
PR: (to open; stacked on #479; #475 closed)

## Goal
D-269: hens roost at night. At dusk (daylight below 0.25) hens walk to the nearest finished HenCoop within 40 m (House building's #479, via `Shape.Entry` then `Shape.Roost`, hens spaced 0.25 m along the roost bar) and settle (`AnimalState.Roost`); with no coop, to the nearest finished roofed building within 25 m, else where they stand. At dawn (daylight above 0.4) they get up, staggered up to 14 s. A coop becomes the flock's home, so they scratch about it by day. Chicks copy their mother's roost. A roosting hen is crouched, and hidden within 0.9 m of a finished coop.

## Progress log
- 2026-10-17: built; live test on a local server: set midnight, hens went to `roost` where they stood (no coop); set noon, they left roost. Coop path not live-tested (needs a built coop). Upgrade check passes. Client hides a roosting hen inside a finished coop and crouches it otherwise. The coop structure and model are House building's #479 (my duplicate in #475 was dropped by PM decision).

## Open questions / blockers
- Needs Audio's `AnimalState.Roost` murmur mapping (told Audio).
- Stacked on #479 (which is on #445): merge those first.

## Next step
Wait for CI, send the PM the green sha, then tell Audio and Modeling the PR is open.
