# model-15: Switchable chibi face states
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/model-15-faces
PR: #644

## Goal
Justin's pick #16: the chibi blinks, has a happy face when eating, and a sleepy face at night or lying down, in the D-313 direction (cutesy, relaxing, with a hint of realism). Modeling gives Animation four switchable faces on every chibi figure: `Face.Open` (today's eyes), `Face.Closed` (a blink), `Face.Happy` (^ ^ eyes and a small open smile), `Face.Sleepy` (half-closed lids and a small "o" mouth), switched with `PlayerModel.SetFace(figure, face)`. Done when the branch is merged and Animation drives the faces in game.

## Contract (agreed with Animation)
- `public enum Face : byte { Open, Closed, Happy, Sleepy }`, top level in the LifeGame namespace (`client/scripts/models/ChibiFigure.cs`). Append-only.
- `PlayerModel.SetFace(Skeleton3D figure, Face face)`: a visibility flip between four MeshInstance3D children (`Face_Open`, `Face_Closed`, `Face_Happy`, `Face_Sleepy`) of a BoneAttachment3D named `FaceAttach` on the Head bone. Safe every frame: the current face is kept in the figure's `face` meta and the call returns at once when it is unchanged, with no allocation. A no-op on figures with no `face` meta (the legacy adult, `--legacy-player`).

## Progress log
- 2026-10-17: The eyes and catch-lights moved off the baked, skinned body mesh into `Face_Open`; the cheek blush stays on the head mesh, shared by every face. `ChibiFigure.Faces(look)` builds the four face meshes per race and skin (unskinned, in the Head bone's frame: model-space points shifted by the Head joint), cached. Lines are brush-stroke tubes laid along the head ellipsoid a little proud of it, thickest in the middle with round tapered ends; the sleepy half-eyes and the mouths are flat fans on the surface. Lids are the skin darkened toward warm brown; the smile is a dark-red D with a pink tongue hint. Kit's mouth would sit on its muzzle. `ChibiFigure.DressFace` adds the attachment in `Create` and only swaps meshes on later calls, so `PlayerModel.SetLook` (race or skin change) keeps the face shown; `Wear` never touches it. Face parts cast no shadow.
- 2026-10-17: Showcase `--view=faces`: Bramble's row above Pebble's in Open, Closed, Happy, Sleepy, close on the heads; `--far` steps back to play distance. The old model-1 style-heads view is now `--view=stylefaces`. Rendered close and far at `--time-of-day=0.55`; all four read at both. `roster` still looks as before with Face.Open. Render: `renders/model-15-faces.png`.

## Open questions / blockers
- (none)

## Next step
PM: review `renders/model-15-faces.png` with Justin and merge claude/model-15-faces once CI is green; Animation then calls `PlayerModel.SetFace` for blinks, eating and sleep.
