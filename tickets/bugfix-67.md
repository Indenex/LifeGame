# bugfix-67: the clay test starts with its materials
Lane: Bug fixes (for Integration)
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-8 (on top of #838, claude/integration-vflv1x)
PR: (after #838 merges)

## Goal
Justin said yes at 15:28 on 2026-10-17 to "Let the clay test bot start with its materials already in hand?". The plain ClayCheck run makes its bot an admin through the CLI (`make_admin`, test databases only) and gives it what a new player would gather: 6 stone, 11+MarkerSticks sticks, 7+MarkerFiber fiber, 12 clay and 1 log. That skips the gathering, digging and axe-and-log stages, and the claim is aimed at the water, which the run walks to next. `--full` keeps the fresh-character path. The clay job gets near 30 s at x60. Only `tools/ClayCheck/Program.cs` changes.

## Progress log
- 2026-10-17: written to Integration's spec; `SetTestSpeed` is now `OwnerCall(reducer, arg)`. Nightly run on the branch to check it.

## Open questions / blockers
- If the bench can't find clear ground with the water-ward claim, drop that step (step 5 of Integration's spec).

## Next step
Check the manual Nightly run on claude/bugfix-rnwmoa-8; open the PR once #838 merges (merge main in first).
