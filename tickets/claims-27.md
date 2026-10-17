# claims-27: Strikes you can feel (playtest strike feedback)
Lane: Claims
Owner: Claims thread (session_01T6RYk3zyUHiSncD3ZXrFFm, back on Justin's first account)
Status: in_review
Branch: claude/claims-nxkyjn
PR: #839

## Goal
Land the playtest strike feedback that was built on the other account but never merged (handoff `/mnt/project-files/handoff/claims-strike-feedback/`, old ticket claims-7 there; that id is taken on main by trading). Main already had the swing on a real hit (anim-5, everyone sees it) and the flinch. This adds what main lacked:
- The strike key with nothing to hit still swings your arm (only you see it; not with a bow in hand). If a person is close ahead, a short note says why: "Too far away", "No fighting here", "<name> is new here. Leave them be for now". Nobody near: no note, so a stray click stays quiet (changed from the handoff, which said "Nothing in reach" on every miss).
- The player strike cone is wider: anyone within 1 m counts, and anyone not well behind you (dot > -0.1, was 0.4).
- A blow you take flushes the screen edge red on the side it came from (fades in about a second) and a note says "<name> struck you" (not on a killing blow; the death card speaks then).
- After the death card, the land chip waits 4 s and fades in over about 2 s, so waking reads calm.
- Claiming too near spawn says "Too close to where newcomers wake up. Claim land a little further out" (the keep-out reaches past the clearing, so "this clearing" was wrong). SocialTest matches.

## Progress log
- 2026-10-17: ported onto current main (SocialLayer.Fighting.cs, SocialLayer.cs, two-line Main hook `social.StrikeBlocked/Facing` = Herd's, Claims.cs wording, SocialTest). No dotnet/spacetime in this container (network blocked), so CI is the first build.

## Open questions / blockers
- Renders of a miss note and a side flush (Render lane, not blocking).

## Next step
Wait for CI on #839 (now also carrying claims-28 and claims-29); send the green sha to the PM.
