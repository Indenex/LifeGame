# skills-9: The journal remembers recipes
Lane: Journal and skills
Owner: Journal and skills thread
Status: in_review
Branch: claude/journal-skills-iseewg
PR: #43

## Goal
In the playtest, new players spent most of their time working out what to do. Once you have discovered something, the journal now tells you how to do it again. Each discovery card gets a short "how" line:
- Crafting jobs: what it needs (and the tool), what it makes, and the key ("Needs 2 sticks and a tinder bundle. Makes a campfire. C to make it.").
- Everything else (digging, foraging, berries, firing pots, kilns, building stages, shearing, hunting): one plain sentence with the key, such as "F digs the ground in front of you; clay lies deeper down."
Keys are read from the live InputMap, so a rebinding shows the right letter. Rank, speed, best quality and who taught you stay as they are.

Also fixes two gaps found while doing this: `carve.*` (digging stick) and `haft.*` (stone knife) trained no skill; they now train Toolmaking. The kiln's processes (`clay.kiln`, `clay.kiln_fire`) get journal names and count toward "still to discover". After main brought in hides (Animals): `hide.scrape`, `hide.stretch` and taking dried leather (`hide.dry`) train Husbandry; `hide.dry` gets the name "Drying leather", a how-to line and counts toward "still to discover".

## Progress log
- 2026-10-16 00:15: PM approved (skills-9). Started.
- 2026-10-16 00:30: Recipes.cs (how-to line per discovery card, keys from InputMap); carve/haft train Toolmaking; kiln names. Client and server build. CraftingUi.ToolName made internal (one-word hook). Screenshot rendering.
- 2026-10-16 00:35: screenshot /mnt/project-files/screenshots/journal-recipes.png; PR #43 opened.
- 2026-10-16 00:28: CI green on b1ef4d7 (https://github.com/Indenex/LifeGame/actions/runs/38828736929); merged main (#38, ef1c26b), builds locally; CI re-running on the merge.
- 2026-10-16 (new session): CI green on 643475a (run 38829787501). Main had moved to 7c65c0f; merged it cleanly. Main added hides (hide.*), which trained no skill and `hide.dry` had no name: hides now train Husbandry (default, sent to the PM as a choice), `hide.dry` named "Drying leather" with a how-to line and counted in "still to discover". No .NET/SpacetimeDB in this container, so CI builds it.
- 2026-10-16 17:12: PM relayed Justin: D-065 hide work gets its own capped Leatherwork specialty later, once leather has more uses; until then it trains Husbandry (as shipped). D-067 toasts only at new titles (as built in skills-8).
- 2026-10-16 17:25: no CI had run on 13fcbce..97cfae1: the PR conflicted with main (STATUS.md), so GitHub skipped the pull_request workflow. Merged main (7e79d22), took main's STATUS.md; no new process ids to name.
- 2026-10-16 17:24: Animals lane (via the PM, D-071): `sew.*` (animals-9: wool tunic, leather wraps) trains Toolmaking; prefix added now so it works when animals-9 lands. A capped Weaver specialty takes spinning, weaving and likely sewing later.

## Open questions / blockers
- D-075 skill curve review (PM asked 17:24), sent to the PM as options. Uses to reach a rank, one skill, ~30 s per use with gathering:
  | Rank | Now | A (rec.) gain=max(3, 30/(1+pts/700)) | B gain=max(2, 20/(1+pts/600)) |
  |---|---|---|---|
  | 15 | 18 (9 min) | 103 (52 min) | 169 (1.4 h) |
  | 20 Apprentice | 25 (12 min) | 162 (1.4 h) | 266 (2.2 h) |
  | 40 Journeyman | 67 (34 min) | 511 (4.3 h) | 860 (7 h) |
  | 60 Expert | 150 (1.3 h) | 1,069 (9 h) | 1,826 (15 h) |
  | 80 Artisan | 394 (3.3 h) | 1,736 (14 h) | 2,826 (24 h) |
  | 100 Master | 794 (6.6 h) | 2,403 (20 h) | 3,826 (32 h) |
  Effects (quality cap 35 to 100, speed to 40% time, minigame steps -75%) stay linear per rank by default; option to soften speed to 60% time and steps to -50%. Stored points are kept, so no wipe; existing players keep their ranks. Default until answered: no change.

## Next step
Wait for CI on PR #43 (head after the 7e79d22 merge); when green, log the run link here and send the PM the sha.
