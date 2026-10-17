# painted-4: Painted animals
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm), Opus helper
Status: in_review
Branch: claude/painted-4-animals (includes painted-1 until #554 lands)
PR: #560

## Goal
Part of the painted rollout (painted-1 rocks ... -6 items): sheep, deer and hens on Painted's shader, so fleece, coats, faces and legs get the stepped light bands, violet shade and soft ink edge. Wool reads soft (low crevice, gentle dabs); the per-sheep fleece tint and per-deer coat still work (one painted twin per source colour). Bone layouts and public APIs unchanged.

## Progress log
- 2026-10-17 05:30: `client/scripts/models/PaintedAnimals.cs`: `Wool(source)` (crevice 0.08, lit_dabs 0.14, dab_size 0.7, ink 1.0, soft violet shade), `Coat(source)` (crevice 0.15, ink 1.3, deeper violet shade, a light lean to a warm card brown) and `Skin` (the vertex-coloured white material for faces, legs, beaks, antlers). `Sheep.WoolLook` returns the painted wool twin (hens use it too), deer coats use `Coat`, and Sheep/SheepFigure/DeerFigure/HenFigure use `PaintedAnimals.Skin` for surface 1. Showcase `animals` view: today's look in the back row, painted in front (two fleece colours, hind, stag, two hens). Build 0 errors; noon render iterated 4 times.

## Open questions / blockers
- (none) Possible follow-up: a fluffier wool silhouette for the painted read.

## Next step
PR after painted-1 (#544 stack) merges; Render before/after of the `animals` view at noon and golden hour.
