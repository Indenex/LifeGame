# survival-23: Rest spots offer to set your waking place
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_review
Branch: claude/survival-rest-spot
PR: (to open; stacked on #546 for the SurvivalCheck cases)

## Goal
Justin: "sleeping mats/beds, etc. should offer to set your respawn point." Before, laying a mat silently made it your waking place (House building's RespawnPointHook). Now:
1. Rest spots are data (D-206): `RestSpotDef` in `shared/RestSpots.cs` (`RestSpots.All`; today the sleeping mat; a future bed is an entry with an item or a `StructureKind`).
2. Explicit offer: laying a mat, or pressing G on your laid mat, opens "Wake here after a fall?" with Set / Not now (and Roll up the mat, which used to be the G action there). Set calls `SetRespawn(spotId, structureId)`, which the server checks (D-182): you must have a right to the building (owner, builder or claim member: `MayLiveIn`), the spot must stand (`RestSpotStands`: building finished and, for the mat, still laid out), and you must be within reach. It writes `RespawnPoint` (private; the client reads `my_respawn_point`). Laying no longer sets it.
3. Feedback: "You'll wake here." note; a ring and "Your waking place" label that fades in near the spot; on death the wake-up line says where you woke: on your chosen spot, or "Your bed was gone; you woke at the edge of the wilderness" (and the stale choice is forgotten). `Vitals.WakeKind` (Default 0) carries which.
4. Light (D-081): no penalty, no cooldown beyond reach.

## Changes outside Survival's files (small, told to House building)
- `BuildingBed.cs`: the `RespawnPointHook` implementation is replaced by `LaidRestSpotHook` (is your mat still laid at that building); header comments updated.
- `Survival.cs`: `Die` now calls `ChooseWake` (RestSpots.cs); the old hook declaration is gone; `Vitals.WakeKind` appended.
- `tools/BuildTest/Program.cs`: `my_respawn_point` subscription and a few checks after the mat step (laying does not set it; B is refused; A's choice is recorded at the mat).

## Progress log
- 2026-10-17: implemented; server and client build; BuildTest mat checks pass locally.
- Not covered by any check: waking after a death on the chosen spot, and "bed was gone" (dying needs a long wait or a fight). The logic is `ChooseWake` in server/spacetimedb/RestSpots.cs.
- Existing players who laid a mat before this lose the silent waking place until they Set it (a one-time change; Justin's world was wiped, so none expected).

## Open questions / blockers
- Prompt look follows UX's shared frame (`Ui.Window`); UX may restyle it.
- A bed that is a structure (not a laid item) needs only its `StructureKind` on a `RestSpotDef` and a G offer for it; the client offer is mat-only today.

## Next step
Open PR, wait for CI, send the green sha to the PM (session_01CvoSQGRe3vDvDXWQu2qruU); tell House building (session_01DD9p6EhSc5pJ8CjNmDFTrG) and UX (session_01V6qgbNbvTwniqW946KACLr).
