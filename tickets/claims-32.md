# claims-32: Friends list
Lane: Claims
Owner: Claims thread
Status: in_progress
Branch: claude/claims-nxkyjn
PR: (opening)

## Goal
Justin picked the friends list (2026-10-17, via the PM). Players mark others as friends. Once both
have marked each other, each sees whether the other is online and how far and which way they are,
in the group list's words ("Mara   40 m north", ui-78). A quiet note says "Mara is here" when a friend
comes online. "/friends" opens the list; you add people by name there, or "Add back" anyone who added you.

## Design
- Mutual only for whereabouts (Justin's pick): a one-way mark shows only names, so nobody is
  tracked by someone they never chose. Spots are rounded to 10 m and refreshed every 5 s (shared/Friends.cs).
- server/spacetimedb/Friends.cs: Friend (private; view MyFriends: mine and who marked me), FriendSpot
  (private; view MyFriendSpots: mutual friends only), FriendSweepTimer/SweepFriends, AddFriend, RemoveFriend.
- shared/Friends.cs HowFar: the ui-78 wording as a shared helper (GroupView could switch to it).
- client/scripts/social/FriendsPanel.cs + SocialLayer.Friends.cs; "/friends" in SocialLayer.Say.
- UX asks: a key for the list and "Add friend" in the player menu (PlayerMenu.cs is UX's).

## Progress log
- 2026-10-17: server, shared, client panel and SocialTest friend checks written on a local branch; #861 merged; applied on main and pushed.

## Open questions / blockers
- (none) Justin picked mutual-only whereabouts (2026-10-17).

## Next step
Get the PR green, then ask Render for a shot of the Friends panel (/friends) and send the PM one message with sha and shot.
