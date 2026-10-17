# anim-3: Tools in hand while working
Lane: Animation
Owner: Animation session (Opus), session_019roNRWQBPw4ozNLDhzseig
Status: done
Branch: claude/anim-3
PR: #372 (merged)

## Goal
anim-2's goals are all merged (station pose, chop swing, breath, sheep motion, worn clothes, flinch: #187 to #358). Next: while a craft job runs, the player holds what they work with. The process's needed tool (ProcessDef.NeedsTool) goes in the right hand, e.g. a stone axe when chopping, a hammerstone when knapping, a flake when scraping. For knapping and carving, the piece being worked (the first Consumes item) goes in the left. Hands empty again when the job ends. Your own tool is the one the server uses (the best-quality item with all the tags, as in Crafting's BestTool). Other players show the plainest item with those tags, because their inventories are hidden. Justin sees it via a Render clip.

## Progress log
- 2026-10-17 00:00: #358 (flinch) merged. Built HeldTools.For(processId, inventory) in client/scripts/animation/HeldTools.cs, plus Avatar.Hold(right, left), which puts ItemModels.HeldMesh under ToolR/ToolL only when the held item changes. AvatarWork.Start sets both hands with the work pose. Client builds.

## Open questions / blockers
- Other players' exact tool: PM picked (a) at 23:55. #372 ships with the plainest-item guess. Crafting will add `Tool` (defaulted) to CraftWork at StartCraft; then a follow-up reads CraftWork.Tool for other players in AvatarWork/HeldTools.

## Next step
Done (#372 merged). Follow-ups live in later anim tickets.

- 2026-10-17 03:55 follow-up (claude/anim-8): CraftWork.Tool is on main (Crafting); other players now show the exact tool from it.
