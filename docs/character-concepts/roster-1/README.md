# Roster 1: handoff pack for Modeling

Justin picked the first player roster from round 4 at round 4's level of cute (locked), then narrowed it to **Pebble and Bramble**. Kit and Capling are deferred for now. Both are part-swaps on one shared base. These are 2D storyboard turnarounds for Modeling to build from in MeshKit. They are not game assets.

| File | What it shows |
|---|---|
| `roster-1-lineup.png` | Both at one scale, next to a 1 m fence and a 0.75 m table |
| `turnaround-pebble.png` | Pebble: front, three-quarter, side and back views, plus a colour key and slot → bone list |
| `turnaround-bramble.png` | Bramble: the same layout |
| `../out/r4-shared-base.png` | The bare base with the 16-bone rig overlay and the slot callouts |

Regenerate everything with `../render.sh`. The roster sheets come from `../make_roster1.py`, which draws on the round-4 base in `../make_round4.py`.

## Shared base

- **Height:** the body stands 1.35 m to the top of the head in game. Parts on the head add to that: Pebble about 1.45 m (daisy) and Bramble about 1.47 m (hair tuft).
- **Proportions:** about 2.8 heads tall. The head is round (slightly wider than tall), on a round torso with stubby arms, mitten hands and short legs.
- **Rig:** Modeling's 16-bone rig. Hips, Spine, Neck and Head, then UpperArm, LowerArm and Hand on each side, and UpperLeg, LowerLeg and Foot on each side. Both share the same walk, work and jump animations.
- **Face:** two dark oval eyes with a light sparkle, short brows, a small mouth line and faint blush. These are small painted shapes, because there are no UVs.
- **Painted look:** each shape has a lit tone, a shade band on the lower right and a soft highlight on the upper left. The rim is soft and violet-shifted, never black.

## Slot table: which parts swap on which bones

No part adds a bone. Every part is a rigid or skinned mesh parented to an existing bone.

| Slot | Bone | Pebble | Bramble |
|---|---|---|---|
| skin colour | whole body | green `#a8c27a` | skin `#f2c9a0` |
| belly / chest | Spine | belly plate `#e8d9a0` | (none) |
| ears | Head | (none) | (none) |
| face add-on | Head | (none) | (none) |
| head-top | Head | small daisy | hair cap with a tuft |
| outfit | Spine and Hips (skinned) | (none) | green tunic, trousers |
| front | Spine / Neck | (belly plate, above) | satchel strap |
| back | Spine | **shell (see below)** | (none) |
| tail | Hips | (none) | (none) |
| feet / hands colour | Foot / Hand | `#8fa866` | boots `#7b4e33` |

The exact colours for each part are in the colour key on that character's turnaround sheet.

## Pebble's shell on the 16-bone rig

- The shell is **one rigid mesh parented to Spine**: a dome behind the torso with a lighter rim band `#c9a46a` around its lower edge. It needs no new bone. It moves with the body when walking and working, the same way a backpack would.
- **Size:** it covers the back from just under the shoulders (about the Neck height) down to the Hips. It sticks out a little past the torso at the sides, so it reads in the front view (see the front view on the sheet). The side view shows its depth: about as deep as the torso.
- **Clearance:** the arms swing in front of and beside the shell, never through it. Keep the dome's front edge behind the shoulder joints, so UpperArm rotations don't clip. If they do, narrow the dome at the top rather than adding a bone.
- **Belly plate:** a separate thin rigid plate on Spine at the front, with painted seam lines. It is not part of the shell, so it can be a colour option.
- **Later options (not needed now):** a shell pattern or colour as a look option; the shell doubling as a visual "backpack" for carried items.

## Notes per character

- **Bramble:** the hair is a cap-shaped mesh on Head with a single tuft on top. The back view shows full hair. Skin and hair colour map onto D-233's first-join picks.

Kit and Capling (round 4) are deferred. Their concept sheets are `../out/r4-kit2.png` and `../out/r4-capling2.png`.
