# model-1 style options (for Justin)

Captured with the Showcase scene (`client/scripts/models/Showcase.tscn`) from the code merged in #172, Vulkan, 1600x900. Today's figure on the left for comparison where shown.

| File | What |
|---|---|
| row.png | Everyone: today's figure, then A Carved, B Soft, C Natural, each with its sheep |
| people.png | The four figures full body |
| faces.png | Close on the heads |
| sheep.png | The three sheep, side on |
| walk.png | The figures mid-stride, from the side |
| clothes.png | Style A in the leaf wrap, then wool tunic and foot wraps (standing, walking, behind) |

- **A Carved** (recommended): flat-shaded facets, chunky 6.2-head figure, carved face; player ~1,300 triangles, sheep ~900.
- **B Soft**: smooth and rounded, big head (5 heads), dot eyes; ~3,000 / ~3,100.
- **C Natural**: lifelike 7.3-head proportions, ears, thumbs, crimped wool and a long-faced sheep; ~4,300 / ~3,300.

All are one draw call per figure. A is in the game now (#172); switching is one line (`PlayerModel.Style`).
