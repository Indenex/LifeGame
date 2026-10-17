# ceramics-19: Hardening pass on Ceramics (D-182) and makers survive the trough and the lamp
Lane: Clay and ceramics
Owner: Ceramics thread
Status: in_progress
Branch: claude/clay-ceramics-19-kfr4kr
PR: (none yet)

## Goal
D-182: the server is the authority. Audit every Ceramics reducer for reach, ownership, claim, cooldown and what it hands back, and fix gaps. First finding: pouring a pot into a trough and scooping pots back out returned the pot without its Wear number, so a pot lost its maker and its slip. Fixed. Set-down lamps now remember the maker too (Lamp.Mark, new column with a default, no wipe) and a picked-up lamp gets it back; a set-down lamp shows its maker's stamp.

## Progress log
- 2026-10-17 08:15: audited TendClay, FirePots, FillPots, UseKiln, the three trough reducers, SetLamp/TendLamp, TakeOffPot, SetMark, SlipPiece. All check reach and claim as needed (feeding someone's lamp is allowed on purpose). Found: trough pour/scoop dropped Wear (fixed); lamps dropped the maker (fixed). Not fixable here: cooking in a marked pot (needs crafting-25), jars and troughs set down from the build menu (needs crafting-25 and Building).
- 2026-10-17 08:15: upgrade check passes (new column has a default). ClayCheck --kiln --jar now sets a mark first and checks the emptied pot keeps its maker (running).

## Open questions / blockers
- Cooking in a marked or slipped pot loses them; set-down jars/troughs show neither. Both wait on crafting-25 (Maker column).

## Next step
When ClayCheck --kiln --jar passes (log /tmp/claude-0/clay_k7.log on the session machine, db c20a), open the PR and send the PM the green sha.
