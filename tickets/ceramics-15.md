# ceramics-15: Maker's mark
Lane: Clay and ceramics
Owner: Clay and ceramics lane (Sonnet session_01Sc3N1RCsurSszfX5YwGzMU)
Status: in_progress
Branch: claude/clay-ceramics-15-kfr4kr
PR: (none yet)

## Goal
Justin's answer (via the PM, 2026-10-17): "Pick from a panel and give them a bunch of different symbols colors, fantasy ones, scientific, etc and a color picker kinda thing. Also, if an item is inspected, it tells you who made it." Done when a player picks a glyph and a colour from a panel at the Potter's bench, the pots they make carry a stamp in that glyph and colour, inspecting any made item says who made it, and it is stored on the server (D-182).

## Design
- 50 glyphs in 5 groups (Nature, Creatures, Runes, Alchemy and science, Geometric) as stroke data in `shared/MarkGlyphs.cs`, ids 0 to 49 append-only. `Marks.Groups / All / Find`. The stamp (MakerStamp atlas) and UX/UI's picker panel draw from the same strokes.
- Colour: packed 0xRRGGBB; `Marks.ClampColor` keeps saturation 0.18 to 0.90 and brightness 0.30 to 0.92 (never neon, nor near black or white); nine earthy presets.
- Server: public `player_mark` (PlayerId, Glyph, Color), validated `SetMark`. No row means no mark.
- Items: a pot carries its maker only, in the per-stack Wear number (bit 16 = made by someone, low 16 bits = the maker's player id). Pots from different makers never stack. The glyph and colour come from the maker's current mark, so changing it restyles older pots.
- Carried by: shaping (ClayPiece.Mark), pick-up, pit fire, the kiln load (kept in its own KilnMarks table so the Kiln row keeps its shape: no wipe), the ground, fill (FillPots) and drink (DrinkFrom).
- Client: stamp drawn in the clay shader (outside surface, twice, opposite sides), pots on the ground in separate batches by maker, the HUD item list says "made by <name>", bench Use prompt "Choose your mark" (`CeramicsView.OpenMarkPicker` hook for UX's panel; a stand-in cycles glyphs).
- Demos: `--ceramics-demo=marks-sheet` (all 50 flat), `--ceramics-demo=marks` (12 sample pots).

## Progress log
- 2026-10-17 03:30: first version (default glyph from id, cycle at the bench) built and the upgrade check found the kiln load list can't take a field; moved the marks to KilnMarks. Then Justin's answer arrived: redesigned to 50 glyphs, free clamped colour, player_mark, maker id on pots. Pushed 1cb35e3; upgrade check vs origin/main passes. API sent to UX/UI (session_01V6qgbNbvTwniqW946KACLr). Render asked for the sheet and sample pots. ClayCheck --pot extended (bad glyph refused, neon colour clamped, pots carry the maker, mark survives fire, pick-up and fill, mark change); c15m2 running.

## Open questions / blockers
- 'Made by' on every made item (tools, garments and so on) needs a real `Maker` column on InventoryItem, WorldItem, BasketItem and ClaimStash, set when a craft finishes, with stack identity: a Crafting and Integration ticket (asked the PM to assign it). Until then only pots carry a maker.
- Cooking in a marked pot loses the mark (the craft doesn't track which pot it used).
- Set-down jars, troughs and lamps don't show the mark yet (needs the structure rows).
- Look: the glyph art is rough line art until Justin picks (D-154).

## Next step
When c15m2 passes: open the PR and send the PM the green sha; then render feedback; then the Maker column once Crafting has it.
