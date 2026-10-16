# skills-9: The journal remembers recipes
Lane: Journal and skills
Owner: Journal and skills thread
Status: in_progress
Branch: claude/journal-skills-iseewg
PR: (not yet)

## Goal
In the playtest, new players spent most of their time working out what to do. Once you have discovered something, the journal now tells you how to do it again. Each discovery card gets a short "how" line:
- Crafting jobs: what it needs (and the tool), what it makes, and the key ("Needs 2 sticks and a tinder bundle. Makes a campfire. C to make it.").
- Everything else (digging, foraging, berries, firing pots, kilns, building stages, shearing, hunting): one plain sentence with the key, such as "F digs the ground in front of you; clay lies deeper down."
Keys are read from the live InputMap, so a rebinding shows the right letter. Rank, speed, best quality and who taught you stay as they are.

Also fixes two gaps found while doing this: `carve.*` (digging stick) and `haft.*` (stone knife) trained no skill; they now train Toolmaking. The kiln's processes (`clay.kiln`, `clay.kiln_fire`) get journal names and count toward "still to discover".

## Progress log
- 2026-10-16 00:15: PM approved (skills-9). Started.

## Open questions / blockers
- (none)

## Next step
Write client/scripts/journal/Recipes.cs and use it in JournalPanel.EntryRow; then screenshot.
