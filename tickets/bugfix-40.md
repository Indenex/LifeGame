# bugfix-40: "Placing a sleeping mat, it just disappears"
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_progress
Branch: claude/bugfix-40
PR: (to open)

## Report
Justin, on a recent release: "when placing a sleeping mat, it just disappears."

## What was checked (2026-10-17)
- Server: the mat is not a `PlaceItem` item (`SleepingMat` is not `Placeable`); it is laid with G inside a finished hut or house (`LayMat`). `LayMat` takes the item and inserts a `SleepingMat` row (public). BuildTest's LayMat stage exercises exactly this and passes: the row exists and the other player sees it, the mat leaves the hands, rolls up again and re-lays.
- Placeable items (PinchPot, CoilPot, Brick, StorageJar, Basket, Log) all have a ground model or the clay-sphere fallback in `ItemLook`/`ItemModels`; none can be set down without something drawn.
- Client: `Subscriptions` includes `sleeping_mat` (BuildingSystem.Use.Queries, public table, no private table in any client subscription: scanned), `BindMats` runs, `ShowMat` builds the mesh on insert.
- Not found by reading: a rule that consumes the mat without a row. The remaining suspect is the picture: the mat was placed 1 cm above the smooth ground height, but the ground is drawn as triangles that can stand above that, and the mat is 1.75 m long.

## Change (a mitigation, not a confirmed cause)
`ShowMat` lays the mat on the highest ground point along its length, plus 3 cm.

## Open question for Justin
Where exactly did he place it (G inside a hut, or dragged from the bag), what message appeared, did the mat leave his bag, did it come back after rejoining, was he on a slope? If it left the bag and the row exists (`spacetime sql ... select * from sleeping_mat`), it is drawing; if the row is missing, the server refused or removed it.

## Next step
Get Justin's answers via the PM; if the row exists, look at the draw path with Render's screenshot; if not, look at the server log for the LayMat call.
