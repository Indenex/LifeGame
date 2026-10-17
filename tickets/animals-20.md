# animals-20: Hen coop and roosting
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-roost-5gkntn
PR: (to open)

## Goal
D-265 and D-269: a small thatched hen coop (claim-only, `StructureKind.HenCoop`, `StructureShape.Coop`) and hens that roost at night. At dusk (daylight below 0.25) hens walk to the nearest finished coop within 40 m and settle (`AnimalState.Roost`); with no coop, to the nearest finished roofed building within 25 m, else where they stand. At dawn (daylight above 0.4) they get up, staggered up to 14 s. A coop becomes the flock's home, so they scratch about it by day. Chicks copy their mother's roost.

## Progress log
- 2026-10-17: built; live test on a local server: set midnight, hens went to `roost` where they stood (no coop); set noon, they left roost. Coop path not live-tested (needs a built coop). Upgrade check passes. Client hides a roosting hen inside a finished coop and crouches it otherwise. Coop model is a rough placeholder (posts, slats, thatch gable) until Modeling's.

## Open questions / blockers
- Needs Modeling's coop model and Audio's `AnimalState.Roost` murmur mapping (told Audio).
- Coop costs (D-075, start low): stage 1 8 sticks + 2 cordage, stage 2 6 fiber.

## Next step
Wait for CI, send the PM the green sha, then tell Audio and Modeling the PR is open.
