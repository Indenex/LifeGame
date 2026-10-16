# bugfix-17: G feeds the fire instead of picking up a pot fired in it
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-17
PR: #99

## Goal
After a pit firing, the news says "Your pinch pot is fired, and whole. G picks it up." Pressing G at the fire picks up the fired pot (or the sherds of a cracked one); it never puts a stick in the fire. Away from fires, G on clay goods works as before.

Cause: the server drops the fired pot where it sat, on the fire ring 0.45 m from the fire's centre (Ceramics.cs DropNear). Facing the fire, both "Feed the fire" (crafting.feed_fire, priority 20, Main.cs) and "Pick up the pinch pot" (ceramics.pick_up, priority 10) are in front of you, so feeding wins. Each G took one stick, fiber or tinder, even with the fire at its 20-minute cap, where the fuel adds nothing. Found in the fresh-world playtest review of the clay path on main 7c65c0f.

Fix (client/scripts/ceramics/CeramicsView.cs): a second handler, ceramics.take_from_fire at priority 24, offers clay goods lying within ClayRules.FireReach (1.8 m) of a fire. It sits above feeding (20) and below taking a roast (25), as a roast already outranks feeding. The plain ceramics.pick_up (10) offers only goods away from fires, so the two never both offer the same thing.

## Progress log
- 2026-10-16: fresh-world bot checks on main all pass (LoadTest, BuildTest, SpawnCheck, CraftCheck; SocialTest only fails without the newcomer-grace setup, which #77 fixed). A code review of the clay path found this bug as the worst; checked by reading Use.Best, Ceramics.cs and Main.cs. Fixed; the client builds.
- 2026-10-16: pushed d368899 (merged with main 679ede5), opened #99. Render requested 679ede5.

## Open questions / blockers
- Idea sent to the PM: should feeding a fire that is already at its 20-minute cap be refused instead of eating the fuel? (separate from this fix)

## Next step
Wait for CI on #99 and record the run link here; then send the PM the green sha.
