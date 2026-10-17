# survival-20: Hide waterskin
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: done
Branch: claude/survival-waterskin
PR: merged, first commit 2041f354

## Goal
Justin picked option 1 of the light-survival options, "water skin only" (no bark bowl): an early water carrier made by hand before pottery, because the walk to water is the first-evening weak spot and you cannot carry any until you fire a pot. Built as data (docs/CONTENT.md): `Item.Waterskin` (161) and `Item.WaterskinOfWater` (162), a `PotDef` first in `Foods.Pots` (so it is drunk before a pot), recipe `hide.waterskin` (sheep hide + cordage, cutting tool, hand craft, Leatherwork family). Filling (FillPots reducer) and drinking (DrinkFrom / UseItem) use the existing server-validated water path; `ClayRules.FilledItem` now reads `Foods.Pots`. Numbers proposed to the PM: carries 0.4 of the thirst bar (pinch pot 0.5, coil pot 1.0), weighs 0.5 empty and 1.5 full (pinch pot 1 and 2), and filling a skin does not train the pottery skill. The empty one folds away (it is a light, small item; no placement).

## Progress log
- 2026-10-17: implemented; server module and client build. Ground look is a stand-in mesh in SurvivalItemLook until Modeling's models land.

## Open questions / blockers
- Needs from other lanes: Crafting to confirm `hide.waterskin` (id, hide + cordage, hand craft); Modeling for empty and full waterskin models (ItemModels, hand and bag icon); Journal for a Things-to-try line and its tier/prerequisites in NextSteps (hide.waterskin comes after a sheep hide).
- No CI proof of the new path beyond builds and the upgrade check; add a CraftCheck step once a hide is easy to obtain in the check.

## Next step
Done and merged; nothing further on this ticket.
