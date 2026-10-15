# skills-7: Skills and journal names for work in flight
Lane: Journal and skills
Owner: Journal and skills thread
Status: in_progress
Branch: claude/journal-skills-iseewg
PR: #34 (merged 2026-10-15 23:41)

## Goal
New processes from other lanes train the right skill and read well in the journal, and count toward "still to discover" once they exist.
- Animals (animals-3): `animal.kill` ("Hunting", reported on every strike that lands) and `animal.butcher` ("Butchering a carcass"), both reducer-reported; Husbandry (survival basic, D-049).
- Crafting (#30): `lash.bow_drill` (Toolmaking) and `fire.bow_drill` (Firecraft) are crafting jobs in Processes.List, so they need nothing here.
- Building: `weave.basket` is a crafting job; the `weave` prefix now trains Toolmaking. Door and lock ids not settled yet (asked Building 2026-10-15 23:13).

## Progress log
- 2026-10-15 23:35: names and to-discover entries for animal.kill/butcher; `weave` mapped to Toolmaking. Bow drill confirmed by Crafting.
- 2026-10-15 23:41: #34 merged; door and lock still waiting on Building.

## Open questions / blockers
- Building: exact ids for the door and lock, and whether they're structure stages (`build.*`, reported) or crafting jobs.

## Next step
Add door and lock names when Building confirms the ids (then a small follow-up PR).
