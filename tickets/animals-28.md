# animals-28: claim rules for eggs and hide-rack leather
Lane: Animals
Owner: Animals thread
Status: done
Branch: claude/animals-claim-rules-5gkntn
PR: #772 (merged 2026-10-17)

## Goal
PM overnight defaults (D-323, for Justin to revisit), from the animals-27 audit: 1a eggs from hens standing inside a claim are for the claim's members (`ClaimAt` + `IsOwnerOrMember`: owner and members the owner added, not townmates, not the banned), open to anyone outside claims; 2a leather on a hide rack is for the player who hung the hide and the claim's members only. Refusals are friendly lines, no penalty: "These hens belong to {owner}'s yard. Ask them for an egg" and "That leather is still drying for whoever hung it". 3a (butchering and shearing) stays as it is.

## Progress log
- 2026-10-17: built on the audit branch (#769); server builds. Not live-tested.
- 2026-10-17 12:52: switched to IsOwnerOrMember (Claims, owner and members only); merged main; CI green after one re-run of an unrelated SocialTest town flake; merged as #772

## Open questions / blockers
- The client still offers "Take eggs" to a non-member in a claim; the server then refuses with the friendly line. A client-side membership check would duplicate the Claims rules, so I left it (the refusal is the check).
- Justin to revisit 1a and 2a in the morning (D-323).

## Next step
Merged. Pick the next Animals item with the PM (D-145); autumn rut and fawn spots stay held for Justin.
