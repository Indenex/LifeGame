# crafting-5: Time-to-fired-pot playtest
Lane: Crafting
Owner: Crafting thread
Status: in_progress
Branch: crafting-pot-playtest
PR: (not yet)

## Goal
Play current main as a brand-new player, from waking up to a fired clay pot, using a bot for timing and the real client for what a player sees. Find where a new player gets stuck or bored (an unclear next step, recipes they can't discover, grindy counts). Fix what's in the Crafting lane and send the PM a short list for the owning lanes, plus the total time-to-pot and screenshots of the friction points.

## Progress log
- 2026-10-15 23:40: started from main 295f672. Ran ClayCheck as the timing bot.
- Fix 1 (crafting): greyed-out recipes now say what is missing and where to get it ("You need 2 more clay. Dig (F) where the ground is grey clay, near water." / "Make it first: Clean clay." / "You need a stone flake. Make it first: Knap a flake.").

- Bot timing (ClayCheck on main 295f672, expert path): clay dug 0:32, tempered clay 1:02, fire lit 1:09, pots shaped ~1:20, pot bone dry 5:28 (246 s beside the fire), fired whole 9:28, filled with water 9:34. About 8 of the 9.5 minutes are drying and firing.
- Fix 2 (crafting): sticks are lighter dry wood and rest on the ground instead of half buried; fiber bundles are bigger and lighter (screenshot 12).
- Fix 3 (crafting, default flagged to PM): a new fire burns 180 s (was 60 s), about the two sticks it was lit with.
- Clay hint wording fixed to match the look: bare tan patches where the grass thins out.
- CraftCheck passes locally.

## Open questions / blockers
- PM/Justin: new fire burn time 180 s (default) vs the old 60 s.

## Next step
Open the PR, send the PM the time-to-pot and friction list, drive CI green.
