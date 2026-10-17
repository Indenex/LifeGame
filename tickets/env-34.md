# env-34: Mixed shapes for rocks, cattails and ferns (D-166)
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-variety
PR: #777

## Goal
Wire Modeling's new variants so no patch is one shape stamped over and over (D-166):
- Boulders (model-28, #760): the kind still follows the region (Regional 94 over
  RockKinds), and each stone takes either form of that kind (Hash01 1294).
- Cattails (model-30, #768): `CattailsOf(Variant(x, z, 32, CattailVariants))` per clump.
- Ferns (model-32, #771): one batch per `FernOf(v)`, picked per cell (Variant salt 33);
  the ferns along a fallen giant pick per fern too (LandmarkProps, salt 625).
- ChunkWorker warms all five cattails and all five ferns on the main thread.
Visual only: collision does not depend on which mesh a stone or plant uses.
Cost: up to 4 more fern draws and 4 more cattail draws per near chunk (merged per mesh in
far regions).

## Progress log
- 2026-10-17 12:35: built; local render running.

## Open questions / blockers
- (none)

## Next step
Wait for PR CI; send the green sha to the PM; Render request (woodland ferns, a reedy bank, a boulder patch).
