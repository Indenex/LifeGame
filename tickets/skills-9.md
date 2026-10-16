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

## Open questions / blockers
- (none)

## Next step
Wait for CI on PR #43 (head after the 7e79d22 merge); when green, log the run link here and send the PM the sha.
