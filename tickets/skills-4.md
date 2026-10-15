# skills-4: Teaching feels social, and a Husbandry skill
Lane: Journal and skills
Owner: Journal and skills thread
Status: in_review
Branch: claude/journal-skills-iseewg
PR: #24

## Goal
When someone learns from you, you find out. Watching: the worker gets a quiet toast, "Ana picked up digging from watching you". Notes: if you're online the author hears at once ("Ana learned digging from your note"), otherwise at their next login ("While you were away, Ana learned digging from your note", or a count when there were several). The journal adds "You have taught N people M things." under the discoveries list.

Also: shearing (`animal.shear`, Animals lane's Shearing.cs) trains a new Husbandry skill (`animal.*`), a survival basic outside the cap (Justin, D-049); the journal names it "Shearing a sheep" and counts it as a thing to discover.

## How it fits together
- `server/spacetimedb/Knowledge.cs`: new private table `Taught` (Teacher, LearnerName, ProcessId, LearnedBy, At, Seen) with view `my_taught`; rows are written by `TeachOnlookers` and `ReadNote` (not for reading your own note). Reducer `MarkTaughtSeen` flips Seen once the client has shown it. Additive schema; no wipe needed.
- `client/scripts/journal/Journal.cs`: subscribes `my_taught`; toast on a new row once caught up; a single "while you were away" toast after the first subscription applies; then calls `MarkTaughtSeen`.
- `client/scripts/journal/JournalPanel.cs`: the taught count next to "N more things to discover".

## Progress log
- 2026-10-15 22:50: started as the default next ticket (proposed to the PM 22:48) while the dug-clay fold-in waits on Ceramics.
- 2026-10-15 22:58: built; scratch harness with three players: B watching A dig and forage gives A two `Taught` rows (Watching), C reading A's note gives one (Reading), `MarkTaughtSeen` sets Seen. Client and server build.

- 2026-10-15 23:00: added Husbandry (capped, `animal` prefix) per the PM's default; screenshot of the toast and journal count for Justin in progress.
- 2026-10-15 23:02: Justin chose Husbandry as a basic (D-049); switched.

## Open questions / blockers
- (none)

## Follow-ups
- Dug-clay fold-in (see tickets/skills-3.md) once Ceramics' PR lands.

## Next step
Wait for CI on the skills-4 PR, then send the green sha to the PM.
