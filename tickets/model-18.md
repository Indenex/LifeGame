# model-18: Wild rabbit figure and items
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/model-18-rabbit
PR: (see below)

## Goal
For Animals' animals-22 (PR #723), D-323.
- `RabbitFigure`: a soft, round wild rabbit about 0.25 m to the top of its head.
  - Long ears with pink insides, big hind feet, a white scut and a pale chin and belly, in the hen and sheep's painted look.
  - Same bones as Sheep.cs: body 0, fleece 1 (tail tuft), head 2, legs 3 and 4 front, 5 and 6 hind.
  - It provides `HeadAt`, `Hips`, `Build(fur)` and `Create(fur)`.
  - `Hop(gait, amount)` gives the body lift and pitch and the front and hind leg swing for a hopping gait, for Sheep.cs to use.
- Item models: raw rabbit 214, roast rabbit 215, burnt rabbit 216 and rabbit fur 217. They're keyed by number until #723 is on main; meat is the Food family, fur Hide.
- Showcase view `rabbit` in `Showcase.Modeling.cs`.
Render: `renders/model-18-rabbit.png`.

## Progress log
- 2026-10-17: built the figure, Hop, the items and the view; build has 0 errors; rendered.

## Open questions / blockers
- Animals wires Model == "rabbit" in Sheep.cs (their file).

## Next step
Wait for CI on the PR, send the green sha to the PM, and tell Animals.
