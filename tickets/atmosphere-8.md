# atmosphere-8: Admin time commands (/day, /night, /time)
Lane: World atmosphere and sound
Owner: Atmosphere thread
Status: in_progress
Branch: claude/atmosphere-sound-hlqfke
PR: (after #52)

## Goal
Justin can set the time of day: `/day` (mid-morning, 0.35), `/night` (0.92) and `/time 0-1` typed in chat, admin only. Every client glides to the new time over about 2 s (the existing clock glide), lighting and ambience included. Admin commands show in F1 for admins only.

## Progress log
- 2026-10-16 01:45: WorldClock.cs: `WorldAdmin` table (public), `IsWorldAdmin` (ClaimConfig.Admin, the publisher, or a WorldAdmin row), `MakeAdmin(playerName)` and `SetTimeOfDay(fraction)` reducers. A game client has its own identity, not the publisher's, so Justin runs once: `spacetime call lifegame make_admin '"<his name>"'`. Client: atmosphere/TimeCommands.cs; one-line hook in SocialLayer.Say (Claims' chat), `KeyHelp.Extra` rows (UX's F1), Atmosphere subscribes to world_admin. Verified on a local server: set_time_of_day 0.9 put DayStart 81 of 90 min back; anonymous caller refused; 1.5 rejected.

## Open questions / blockers
- (none)

## Next step
When #52 merges: merge main in, push to the assigned branch, open the PR, send the green sha to the PM.
