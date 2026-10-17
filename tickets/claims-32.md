# claims-32: Friends list
Lane: Claims
Owner: Claims thread
Status: in_progress
Branch: claude/claims-nxkyjn (new PR from main once #861 merges)
PR: (not yet)

## Goal
Justin picked the friends list (2026-10-17, via the PM). Players mark others as friends. Once both
have marked each other, each sees whether the other is online and how far and which way they are,
in the group list's words ("Mara   40 m north", ui-78). A quiet note says "Mara is here" when a friend
comes online. "/friends" opens the list; you add people by name there, or "Add back" anyone who added you.

## Design
- Mutual only for whereabouts (default, sent to the PM): a one-way mark shows only names, so nobody is
  tracked by someone they never chose. Spots are rounded to 10 m and refreshed every 5 s (shared/Friends.cs).
- server/spacetimedb/Friends.cs: Friend (private; view MyFriends: mine and who marked me), FriendSpot
  (private; view MyFriendSpots: mutual friends only), FriendSweepTimer/SweepFriends, AddFriend, RemoveFriend.
- shared/Friends.cs HowFar: the ui-78 wording as a shared helper (GroupView could switch to it).
- client/scripts/social/FriendsPanel.cs + SocialLayer.Friends.cs; "/friends" in SocialLayer.Say.
- UX asks: a key for the list and "Add friend" in the player menu (PlayerMenu.cs is UX's).

## Progress log
- 2026-10-17: server, shared, client panel and SocialTest friend checks written on a local branch; waiting for #861 to merge before pushing (one branch).

## Open questions / blockers
- Mutual-only whereabouts is the default; Justin may prefer one-way.

## Next step
After #861 merges: merge main into claude/claims-nxkyjn, apply these commits, push, open the PR, request a render of the Friends panel.
