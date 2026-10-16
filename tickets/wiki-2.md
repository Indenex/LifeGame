# wiki-2: Rewrite the player wiki in a plain voice
Lane: Wiki
Owner: Wiki session (Opus, D-064)
Status: done
Branch: claude/wiki-2
PR: #87

## Goal
Justin found the first wiki (wiki-1, #83) oddly affectionate. Rewrite every page in `wiki/` in a plain, direct voice, like a well-written game manual: short declarative sentences, second person, no cosy or sentimental tone, no "be kind", no cute one-liners ("Look at the sheep; they look at you too"), no exclamation. Keep the same page set and coverage, check facts against `DECISIONS.md` (through D-064) and `KEYMAP.md` on main, and keep the D-063 spoiler rule: direction and hints, no exact numbers, recipes or minigame solutions. Done when the PM has reviewed it, Justin has OK'd the tone, and it is merged; the PM republishes the web page.

## Progress log
- 2026-10-16: ticket written by the PM (D-064).
- 2026-10-16: started on claude/wiki-2 (harness gave a detached HEAD, no branch name); facts checked against DECISIONS.md D-001..D-064 and KEYMAP.md.
- 2026-10-16: full rewrite of all 11 pages pushed (f44a82a, 4961468 + follow-up). Added missing facts: Shift+G bar, Z strike, Ctrl master-play, learning by watching/notes, basket and claim-owner rules, tree felling.
- 2026-10-16: opened PR #87; sent the PM the link, three sample sentences and a terseness question.
- 2026-10-16 17:00: #87 merged; Justin chose "As is" for the tone; PM republished the web page. Done.
- 2026-10-16 17:15: PM: the wiki must match main. Removed the sleeping mat (#61), wetness (#75) and tree felling (#70) lines; none is on main yet. D-065..D-068 need no wiki text (the wiki does not mention skill toasts).
- 2026-10-16 17:35: PM: #75 wetness, #70 chopping, #71 chat bubbles and /me, #47 fire time-left prompt merged; added their wiki lines (no numbers). Sleeping mat (#61) still pending.
- 2026-10-16 18:05: #66 (pond shimmer, rain rings, frogs, clouds, dawn mist) merged; client-side looks only, no rule or key changes, so no wiki text. Pending, add only when the PM says each PR merged: run on Shift with stamina and sneak moving to Ctrl (D-088, D-091), I opens the equipment panel, fires last longer with Firecraft (D-090), sleeping mat (#61).
- 2026-10-16 18:35: PM: merged #61 mat, #118 sneak on Ctrl, #92 cold hearths and clear-ground fires, #43 journal how-to-again lines; added wiki lines. #113 groves, #110 trade server (no window yet), #99/#109 prompt fixes, #66: no wiki text. Still pending: run on Shift (#119), equipment panel on I, Firecraft fire length (D-090), trade window (#111).
- 2026-10-16 18:45: PM: merged #124 (fire warms you quickly, chill capped, basket raises less) and #121 (others see you sneak); added wiki lines. Basket change needs no text (wiki gives no amount).
- 2026-10-16 18:45: #146 merged. PM: send green shas to the PM, who merges. Dropped "quickly" from fire warmth (D-103 is about a minute); rides the next wiki PR.
- 2026-10-16 19:05: PM: merged #111 trade window, #144 trade bubble, #128 Options sound sliders, #125 better tools faster; added wiki lines. #120 work sounds and #115 fences hold sheep (fence building not merged): no text yet. Still pending: run on Shift (#119), equipment panel on I, Firecraft fire length (D-090), fence building.
- 2026-10-16 19:15: PM: merged #119 run on Shift with stamina bar, #105 storage jar (kiln only, quality sets size), #136 forest light (looks only, no text); added wiki lines to PR #163.
- 2026-10-16 19:30: #163 merged. PM: merged #167 (new-arrival slower cold with sprout badge; Endurance skill trained by running) and #108 (pot and fire fixes, no text); added wiki lines. Pending: #175 new land with hills, stream and ponds (world wipe).

## Open questions / blockers
- (none) Justin chose "As is" (a).

## Next step
Wait for the PM to report a merge. Then move the matching draft from wiki/drafts/ on claude/wiki-drafts (crafting stations, new land #175, seasons, camp names), or write fresh lines (equipment panel on I, Firecraft fire length D-090, fence building), into the live pages on claude/wiki-2 restarted from main. Check against the code, no numbers, then open a PR and send the PM the link and green sha.
