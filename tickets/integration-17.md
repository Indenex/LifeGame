# integration-17: Showcase views per lane
Lane: Integration
Owner: Integration thread
Status: in_review
Branch: claude/integration-showcase
PR: (see STATUS.md)

## Goal
`Showcase.cs`'s view list was a conflict hotspot (#479, #623, #661). Views now register through `[ShowcaseView("name")]` and are found by reflection; every existing view is converted in place, and lanes add new ones in `Showcase.<Lane>.cs`. No change to what any view renders.

## Progress log
- 2026-10-17: converted all 23 views, client builds; docs/CONTENT.md updated.

## Open questions / blockers
- None. Not run in Godot (no Godot in this container); Render should capture one view per kind after merge.

## Next step
Wait for CI on the PR, then send the green sha to the PM.
