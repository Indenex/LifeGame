# Character concepts, round 2: one shared base

Justin asked for "another 10. Multiple character models/themes would be nice too I think. I do want a shared model/skeleton etc. though as much as possible to minimize modeling work." These are fast 2D storyboard sketches, not game code.

- **Shared base sheet:** `out/r2-shared-base.png`. It shows the one body, the 16-bone rig and the part slots.
- **Lineup:** `out/r2-lineup.png`. It shows the 10 new concepts, then Kit, Capling, Bramble and Clover from round 1 redrawn on the same base.
- **Per-concept sheets:** `out/r2-<name>.png`, each with front and three-quarter views beside a 1 m fence post.
- **Source:** `make_round2.py`, where every concept is a call to `base()` with slot values. That is literally the "Def entry plus parts" shape of D-206. Regenerate with `./render.sh`.

## The shared base

- **Body:** one round chibi body, 0.84 m to the head top, about 2.3 heads tall. It has a big round head, a round torso, stubby arms with mitten hands, and short legs.
- **Skeleton:** Modeling's 16-bone rig. That is Hips, Spine, Neck and Head, plus upper arm, lower arm and hand on each side, and upper leg, lower leg and foot on each side. The same walk, work and jump animations apply to every concept.
- **Height band:** 0.84 m bare, up to 1.10 m with ears or a hat. Because everyone shares one body, the whole base can be scaled uniformly, for example to about 1.3 m as Modeling recommends for doors and stations, without breaking animations.
- **Slots:** none of them adds bones.
  - head-top: hat, cap, crest, flame, cloud, spikes
  - ears: triangle, round, long, tufts, antennae
  - face: eyes, muzzle, beak, face pattern
  - front: scarf, collar, ruff, held item
  - outfit: tunic, vest, overalls
  - back: wings, shell, pack, all static
  - tail: rides on Hips
  - colour: skin, belly, hands and feet colours, plus pattern

## 10 new concepts (recommended first)

| # | Name | Theme | Slots used | Pitch |
|---|---|---|---|---|
| 1 | **Mitten** | Cat-folk | triangle ears, tabby stripes, muzzle with whiskers, curl tail, bell collar | A lilac-grey tabby with a cream muzzle and a red bell collar. It's the most universally loved animal, its silhouette reads easily, and coat colours make great options. |
| 2 | **Honey** | Bear cub | round ears, muzzle, honey-pot pack, scarf | A brown cub with a red scarf and a honey pot on its back. It's the cosiest shape of all and a natural fit for foraging and cooking. |
| 3 | **Tuft** | Owl-folk | feather-tuft ears, eye discs, beak, wing-coloured arms, chest chevrons | A round brown owl with big eye discs. Its arms read as folded wings, so it's a bird with no new bones. It suits a wise, journal-keeping type. |
| 4 | **Quill** | Hedgehog | spiky head-top, back spikes, button nose, vest | A spiky brown hedgehog with a soft tan face and a green vest. It reads instantly, it's very cute, and the spikes cost nothing to animate. |
| 5 | **Ripple** | Otter | small round ears, cream face mask, thick tail, held shell | A sleek brown otter holding a pink shell. It's a cheerful water and fishing fit. |
| 6 | **Nutkin** | Acorn folk | acorn-cap hat, freckles, leaf tunic | A little acorn person with a hatched cap, freckles and a leaf tunic. It's a plant sprite on a humanoid face. |
| 7 | **Dusk** | Moth | feathery antennae, static back wings, fluffy ruff | A lilac moth with soft wings and a cream ruff. It's dreamy, with a night theme. Its wings are static, but could flutter later with one bone. |
| 8 | **Wick** | Lantern folk | candle-flame head-top with glow, wax drips, vest | A waxy candle person with a little flame on top. Its glow is lovely at night and in caves, and it ties into fire. |
| 9 | **Nimbus** | Cloud sprite | cloud-puff head-top, star clip, puff collar | A pale-blue sky sprite with cloud hair and a star clip. It has a gentle, airy feel. |
| 10 | **Pebble** | Tortoise | static back shell, belly plate, head flower | A green tortoise with a domed shell and a tiny flower on its head. It's slow and steady, and the shell could double as a backpack. |

## Round 1 redrawn on the base

Kit (fox), Capling (mushroom), Bramble (chibi human) and Clover (rabbit) all fit the same body with part swaps only. So a mixed roster of themes costs one body plus parts.

## Suggested question for Justin

"Pick the themes for the first roster: (a) a mixed roster where players choose a theme at first join, starting with Mitten, Honey, Kit and Bramble [recommended]; (b) one theme only; (c) a different mix by number."
