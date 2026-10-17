# painted-18: Softer rock shade (no lilac rocks by day)
Lane: Modeling
Owner: Modeling lane (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/painted-18-rock-shade
PR: #807

## Goal
Environment found pale rocks read pink-lilac at every hour, not only at golden hour (painted-17 / #802 covers that), and most on the tall and split forms with many faces turned from the sun. Cause: Painted.Rock's strong blue-violet shade_tint (0.56, 0.53, 0.8) under today's warmer sun and rim. It goes to (0.66, 0.64, 0.76): still a hint of the cards' violet in the shade, lighter and greyer. Lit faces are unchanged. Render: renders/painted-18-rock-shade.png (main above, this branch below, noon).

## Progress log
- 2026-10-17 14:10: changed and rendered; local build 0 errors.

- 2026-10-17 14:20: world renders at noon (--time-of-day=0.5), main left and this branch right: renders/painted-18-noon-world.png and painted-18-noon-rock.png (the big rock's shaded side goes from violet to a soft grey). Also renders/painted-17-golden-rock-main-802-neutral.png for Justin: the big rock at 0.72 on main before #802, with #802, and with the shade forced neutral (no further change, so the leftover golden-hour pink is light and base colour). CI on 097b7f19 green: https://github.com/Indenex/LifeGame/actions/runs/39039050895

## Open questions / blockers
- A look change Justin may notice (the cards' violet shade). Options sent to the PM: softened (this PR, recommended), keep the deep violet, or a plain warm grey.

## Next step
Wait for CI and the PM's call, then send the green sha.
