# bugfix-56: Hidden values, family 3 (ClayPiece)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_progress
Branch: local commit stacked on claude/bugfix-rnwmoa (#830); patch in /mnt/project-files/bugfix/bugfix-56/. Needs its own branch or #830 merged first.
PR: (none yet)

## Goal
Part of audit item 7 (tickets/bugfix-53.md). Other clients no longer read the quality of clay drying or firing in the open. The maker's mark stays public.

- New private `ClayPieceQuality` (PieceId, Quality) in Ceramics.cs. `InsertClayPiece` writes 0 to the public `ClayPiece.Quality` column (kept: removing a column doesn't migrate) and the real value to the private row; `ClayQuality` reads it (falls back to the public column for old rows); `DeleteClayPiece` removes both.
- `EnsureClayQualityHidden` (on connect) moves the quality of pieces set down before this into the private table.
- No client or tool reads `ClayPiece.Quality` (checked client/scripts/ceramics and tools/ClayCheck).

## Progress log
- 2026-10-17 15:25: written; not built locally (no toolchain here). CI and the Upgrade check are the build checks.

## Open questions / blockers
- Follow-up family: `Kiln.Load` (public) carries each loaded pot's Quality too (Kiln.cs KilnPot). Same treatment later.

## Next step
Push to a branch, open the PR, wait for CI (incl. ClayCheck and the Upgrade check), tell Ceramics through the PM.
