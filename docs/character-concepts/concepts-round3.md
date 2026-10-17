# Character concepts, round 3: less cute, closer to the reference art

Justin's feedback on round 2: "let's go for less cutesy. I also want the concepts to match closer to the reference art." These are fast 2D storyboard sketches, not game code.

- **Lineup in a reference-style scene:** `out/r3-lineup.png`. It shows the shared base and all 7 concepts in a painted pine meadow with grey-violet rocks and a stump, next to a 1 m fence and a 0.75 m table.
- **Shared base:** `out/r3-shared-base.png`. It shows the bare body and the 16-bone rig, with 1.35 m, 1 m and 0.75 m height lines.
- **Per-concept sheets:** `out/r3-<name>.png`. Each has front and three-quarter views, and each is **paired with a crop of the reference it draws from** (the crops are in `out/refs/`).
- **Source:** `make_round3.py`. Regenerate with `./render.sh`.

## What changed from round 2

- **Proportions:** a smaller head, about 4 heads tall instead of chibi (round 2 was about 2.3). The body stays 1.35 m (D-249/D-250) on the same 16-bone rig.
- **Faces:** small eyes with brows, a hint of a nose and a mouth line. Charming, but not baby-like.
- **Silhouettes:** sturdier, with broad shoulders, boots, belts and worn travelling gear such as cloaks, straps, satchels, aprons and plate.
- **Painting:** shapes are painted in 2–3 tone bands with soft violet-brown rims and no hard black lines.
- **Palette:** taken from the reference README. Olive and moss greens, warm browns, grey-violet stone and steel, blue-violet shade and warm linen.

## Shared base and slots

The base is one body, 1.35 m tall, on Modeling's 16-bone rig (Hips, Spine, Neck and Head, plus three bones per arm and three per leg).

| Slot | Options used in this round |
|---|---|
| head | skin or fur colour, ears, snout or beak, face pattern |
| head-top | hood, helm, hair, mushroom cap |
| torso layer | apron, chest plate, robe, coat, vest |
| front | straps, scarf, satchel |
| back | cloak, shield, basket, bow (all static) |
| tail | on Hips |
| held | staff, sword, hammer, lantern |

There are no wings, no four-legged bodies and no fingers.

## Concepts (recommended first)

| # | Name | Theme | Reference | Pitch |
|---|---|---|---|---|
| 1 | **Wayfarer** | Human traveller | 02 meadow | A hooded traveller in a moss-green cloak and rust sleeves, with a walking staff and satchel. The everyman hero, in the forest's own colours. |
| 2 | **Warden** | Armoured traveller | 06 armoured figure | Pale steel plate over a blue tunic, an open helm, a round wooden shield on the back and a short sword. The closest match to the reference figure. |
| 3 | **Fennick** | Fox-folk ranger | 03 forest paths | An upright fox with a longer snout and small eyes, a blue scarf, a green jerkin, a bow on the back and a tail on Hips. The animal-folk option, done grounded. |
| 4 | **Stonebeard** | Stout smith (small folk) | 06 barrels and wood | Broad shoulders, a braided ginger beard, a leather apron over grey-violet wool, and a hammer at the belt. A crafter. |
| 5 | **Bramblehide** | Bear-folk forager | 02 stump and rocks | A big bear in a grey-violet vest and moss trousers, with a wicker basket of greens and berries on its back. A gentle giant forager. |
| 6 | **Capwright** | Mushroom-folk wanderer | 01 grass block | A wide weathered rust-red cap worn like a hat, a long grey-violet coat and a lantern. Reads as a mushroom without being a mascot. |
| 7 | **Hollis** | Owl-folk scholar | 05 snowy pines | A brown owl with eye discs, round brass spectacles, a long blue robe and a satchel of scrolls. A quiet bookish type. |

Mix: 4 humanoid (Wayfarer, Warden, Stonebeard and Capwright, which has a human face under its cap) and 3 creature-folk (Fennick, Bramblehide, Hollis).

## Suggested question for Justin

"Which direction do you want for the first player looks? (a) A mixed roster on one base, starting with Wayfarer, Warden and Fennick [recommended]; (b) humans and folk only; (c) your own picks by number."
