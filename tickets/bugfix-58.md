# bugfix-58: Claim zone rings while placing a claim marker
Lane: Bug fixes (for Claims, which isn't running)
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-3
PR: (see PM)

## Goal
Justin: "when placing a claim it should show the zone ring so you can put a claim next to people." While a claim marker is being laid out, the new claim's ring is drawn on the ground around the ghost: green where the land is free, red where it would overlap another claim (standing, or a marker still being built) or the spawn clearing. The rings of nearby claims are drawn too (cream for claims you belong to, rust for others), plus the spawn clearing's edge when it's close. A spot the server would refuse turns the ghost red and shows the server's own wording in the prompt, so the click isn't wasted.

- `client/scripts/social/BuildingSystem.ClaimRings.cs` (new, Claims' folder like ClaimFences.cs): `ClaimRingHook`, the same rules as Claims.cs `CheckStructureSite`.
- `client/scripts/building/BuildingSystem.cs`: the hook declaration plus two calls (UpdateGhost, StopPlacing).

## Progress log
- 2026-10-17 16:05: written; no local build (toolchain downloads blocked). CI is the build check. Render before/after requested through the PM.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the PM the green sha.
