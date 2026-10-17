# bugfix-56: Hidden values, family 3 (clay pieces and kiln loads)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_progress
Branch: local commit stacked on claude/bugfix-rnwmoa (#830); patch in /mnt/project-files/bugfix/bugfix-56/. Needs its own branch or #830 merged first.
PR: (none yet)

## Goal
Part of audit item 7 (tickets/bugfix-53.md). Other clients no longer read the quality of clay drying or firing in the open. The maker's mark stays public.

- New private `ClayPieceQuality` (PieceId, Quality) in Ceramics.cs. `InsertClayPiece` writes 0 to the public `ClayPiece.Quality` column (kept: removing a column doesn't migrate) and the real value to the private row; `ClayQuality` reads it (falls back to the public column for old rows); `DeleteClayPiece` removes both.
- `EnsureClayQualityHidden` (on connect) moves the quality of pieces set down before this into the private table.
- Kiln loads: `KilnPot.Quality` in the public `Kiln.Load` is written 0; the real values live in the private `KilnQualities` (StructureId, List<byte>), kept in load order like `KilnMarks`. Pots loaded before this fall back to their public value. Deleted wherever `KilnMarks` is.
- No client or tool reads `ClayPiece.Quality` or a kiln pot's quality (the client reads only `Load.Count`) (checked client/scripts/ceramics and tools/ClayCheck).

## Progress log
- 2026-10-17 15:25: written; not built locally (no toolchain here). CI and the Upgrade check are the build checks.

- 2026-10-17 15:40: kiln load qualities added (Kiln.cs). Waiting for #830 to merge so this can go on claude/bugfix-rnwmoa.

## Open questions / blockers
- (none)

## Next step
Push to a branch, open the PR, wait for CI (incl. ClayCheck and the Upgrade check), tell Ceramics through the PM.
