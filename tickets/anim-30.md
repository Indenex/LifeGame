# anim-30: paired emotes (high-five and hug)
Lane: Animation
Owner: Animation thread (session_019roNRWQBPw4ozNLDhzseig)
Status: blocked
Branch: (none yet)
PR: (none)

## Goal
Two players high-five or hug. A offers from the emote wheel while facing B within 2.5 m, and holds an offering pose (a palm raised, or arms opening). B sees a small prompt over A and accepts with the interact key. Both face each other and play the paired motion in step. Declining is passive: B walks away or ignores it, and there is no explicit "no".

Proposed server rules (sent to the PM 2026-10-17; the server is the authority, D-182):
- **OfferPair(target, kind):** both players exist and are within 2.5 m. Neither is resting, swimming, flying or in a craft job, and the target is not the caller. The 1.5 s emote cooldown applies. It writes a private EmoteOffer row {From, To, Kind, At}; each offerer has one live offer, and a new one replaces the old.
- **AcceptPair(from):** the offer is under 10 s old, and both players are still within 2.5 m and free. It deletes the offer and notes a PlayerAct for both with a shared start, and both get the cooldown.
- **Expiry:** an offer expires at 10 s, or when the players move more than 4 m apart or one starts a job. At most 3 offers to the same target per minute.
- **Visibility:** the target sees offers made to them (a view filtered by Me). Bystanders see only the offering pose.

Size: about two tickets. Server: two reducers, an offer table and a view. Shared: EmoteDef entries with a Paired flag. Client: 4 appended WorkMotion entries with poses and a prompt. UX: a wheel tweak.

## Open questions / blockers
Waiting on Justin through the PM (recommended answer first):
- Q1. Which emotes first? (a) high-five and hug; (b) high-five only; (c) add a fist bump.
- Q2. How does B accept? (a) the interact key near A; (b) B picks the same emote on the wheel.
- Q3. Offer range? (a) 2.5 m; (b) 5 m with a walk-up.
- Q4. A hug to anyone? (a) anyone, with passive decline; (b) house-mates only, later.

## Next step
Wait for Justin's answers through the PM. Then branch off main as claude/anim-30 and build the server reducers first, per the rules above.
