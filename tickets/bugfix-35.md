# bugfix-35: BuildTest bots leave the hut before the mat stage
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-35
PR: (to open)

## Goal
Main run 38994774809 (5485ef48): Building check failed at "Sleeping mat" (`gathered fiber for a mat (Fiber:1)`). The server's walls and shut door (bugfix-27, #394) keep the bots inside the clay hut; #394 only made them leave before the Hearth stage, and the mat stage comes first (HangDoor, LayMat, BuildHearth).

## Change
`LeaveHut(hut)` in tools/BuildTest/Program.cs: walk to the doorway, open the door if shut, walk both bots out to the yard. Called at the start of LayMat and BuildHearth.

## Progress log
- 2026-10-17: written; local BuildTest run in progress.

## Next step
PR, wait for CI, send green sha to PM and Integration.
