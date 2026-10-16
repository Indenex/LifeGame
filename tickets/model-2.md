# model-2: Stations, jar, cooking pot, worn clothes, new-land props
Lane: Modeling
Owner: Modeling session session_01JKX3FNjkrEkAqoTaFJiXAm (Opus)
Status: in_progress
Branch: claude/model-2 (stacked on claude/model-1-hearth, which is stacked on model-1's PR #172)
PR: (once #172 merges; send green shas to the PM, D-119)

## Goal
Real models, in the model-1 style (A Carved by default) and built with `client/scripts/models/MeshKit.cs`, for: the new stations (Crafting Table, Potter's bench, distaff stand, drying rack with hides), the storage jar, the cooking pot, the tunic and foot wraps shown worn on the player figure (Garments.cs / PlayerModel.Wear, wired by Animation), and new-land props with the Environment lane. Also the cold hearth (HearthModel.cs, on claude/model-1-hearth). Each lands as a small hook in the owning lane's view file, and I tell that lane first. Justin sees renders through the Render lane, but renders never hold up merges (D-142). Never idle (D-145).

## Progress log
- 2026-10-16 21:45: started from the PM's D-145 message. Branch claude/model-2 from claude/model-1-hearth. A Sonnet survey of where each station is drawn today (crafting-stations, ux-ui-station, anim-1-stations, crafting-cook, environment-env-1/2 branches) is running.

## Open questions / blockers
- Most stations are on other lanes' unmerged branches; hooks wait until those merge, so models land first as standalone meshes plus Showcase views.

## Next step
Read the station survey, then build `StationModels.cs` (Crafting Table first), with a Showcase view.
