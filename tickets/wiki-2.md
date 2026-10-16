# wiki-2: Rewrite the player wiki in a plain voice
Lane: Wiki
Owner: Wiki session (Opus, D-064)
Status: in_review
Branch: claude/wiki-2
PR: #87

## Goal
Justin found the first wiki (wiki-1, #83) oddly affectionate. Rewrite every page in `wiki/` in a plain, direct voice, like a well-written game manual: short declarative sentences, second person, no cosy or sentimental tone, no "be kind", no cute one-liners ("Look at the sheep; they look at you too"), no exclamation. Keep the same page set and coverage, check facts against `DECISIONS.md` (through D-064) and `KEYMAP.md` on main, and keep the D-063 spoiler rule: direction and hints, no exact numbers, recipes or minigame solutions. Done when the PM has reviewed it, Justin has OK'd the tone, and it is merged; the PM republishes the web page.

## Progress log
- 2026-10-16: ticket written by the PM (D-064).
- 2026-10-16: started on claude/wiki-2 (harness gave a detached HEAD, no branch name); facts checked against DECISIONS.md D-001..D-064 and KEYMAP.md.
- 2026-10-16: full rewrite of all 11 pages pushed (f44a82a, 4961468 + follow-up). Added missing facts: Shift+G bar, Z strike, Ctrl master-play, learning by watching/notes, basket and claim-owner rules, tree felling.
- 2026-10-16: opened PR #87; sent the PM the link, three sample sentences and a terseness question.

## Open questions / blockers
- Sent to the PM for Justin: how terse should it be? (a) as is (recommended), (b) terser, mostly bullets and tables, (c) one scene-setting line per page. Working on (a).

## Next step
Wait for the PM/Justin's tone verdict on the PR; apply any requested wording changes on claude/wiki-2.
