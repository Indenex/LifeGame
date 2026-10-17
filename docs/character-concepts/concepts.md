# Character concepts: storyboard (fast mockups)

Justin asked for about 10 quick proposals for shorter, cuter player characters, including creature types. These are 2D storyboard sketches only. Nothing is built into the game. Justin picks 1 to 3 to develop. Under D-154, the chosen style is not built past a rough prototype until he picks.

- **Lineup at one scale:** `out/lineup.png`. It shows a 1 m fence post, the current 1.8 m player, then all 10 in recommended order.
- **Per-concept sheets:** `out/<name>.png`, each with a front view, a three-quarter view and a 1 m fence post.
- **Regenerate:** `./render.sh` (needs python3, ImageMagick and the pre-installed Chromium). `make_concepts.py` draws them as SVG, in metres.

Style notes: the painted look follows `docs/art-reference`. Each shape has a violet-shifted dark rim, a crisp shade band and a soft highlight, with warm pastel accents. Every concept has a head-to-body ratio of 1:1 to 1:2, big dark eyes with sparkles, blush cheeks, and one bold feature that reads at a distance: ears, a cap, a hat or a leaf.

## Recommended picks first

1. **Kit (fox-folk), 0.95 m, about 2 heads.** A curious traveller in a blue scarf. Orange with a cream muzzle and belly, dark ear tips and boots. The tall ears and big tail make an unmistakable silhouette. This is the warmest Dofus-like option, and it fits forest life and foraging.
2. **Capling (mushroom sprite), 0.82 m.** A small forest-floor spirit whose head *is* a red-and-cream spotted cap, which is about 40% of its height. It has a cream stem body and stubby boots. It reads instantly from above with the follow camera and sits naturally in the mushroom-strewn reference meadow.
3. **Bramble (chibi human), 1.10 m, about 2.7 heads.** The safe choice: a big-headed young adventurer in a moss tunic with a satchel and messy chestnut hair. It keeps D-233's skin, hair and beard choices meaningful (they map one to one) while becoming short and cute.
4. **Clover (rabbit-folk), 1.0 m including ears (body 0.7 m).** Round and soft, oat-white with pink inner ears, one ear flopped, in mustard overalls. The ears give height variety. Fur colour and outfit make easy character-creator options.
5. **Pip (gnome or hearthfolk), 0.80 m.** A classic garden gnome with a tall red floppy hat and gold pompom, a fluffy white beard, a big nose and a blue coat. Very readable, with a craftsman vibe that suits crafting and building.
6. **Mossle (mole-folk), 0.75 m.** A pear-shaped velvet-brown digger with big pink paws, a pink snout, happy squint eyes and a miner's cap with a lamp. The lamp glow pays off at night and in caves. It's a fun pick for digging and mining.
7. **Puddle (frog-folk), 0.72 m.** A chubby green frog with a pale belly, eye bumps on top and a wide smile, wearing a lily-pad hat with a pink flower. Suits water and fishing play.
8. **Sprout (seedling sprite), 0.72 m.** A pale-green bean body in a leaf skirt, with a two-leaf sprout on its head. The sprout could grow with progression (a seedling, then a bud, then a flower).
9. **Dumpling (blob creature), 0.62 m.** A soft lilac mochi-blob with nub arms, a little peak on top and a tiny backpack. It's the cutest and simplest to animate (squash and stretch), and colour variants are trivial. The risk is that it reads as a pet more than a player.
10. **Hearthling (wild: clay golem), 0.90 m.** A little jar-bodied clay figure with a moss cap, glowing ember eyes and an ember heart showing through a crack. It ties into ceramics and fire. It's the most distinctive option, but also the least "cuddly".

Spread covered: chibi human (3), small folk (5), animal-folk (1, 4, 6, 7), plant or mushroom sprites (2, 8), blob (9) and wild (10).
