# animals-3: Hunting sheep for meat and hide (D-039)
Lane: Animals
Owner: Animals thread (session session_01XS7M1yLy2fLaFdE3HWnisM)
Status: in_review
Branch: claude/animals-sheep-5gkntn (developed locally as animals-3-local until #23 merged)
PR: #23 (shearing and hunting together, per the PM)

## Goal
Sheep can be killed with a spear or blade after a short chase, lie down calmly where they fall (no gore, D-036), and be butchered with a blade for Raw Mutton and a Sheep Hide at a quality set by tool and Husbandry skill. Flocks that lost animals slowly regrow with lambs. Done when Justin can hunt, butcher and cook a sheep in a build.

## How it works
- Server `server/spacetimedb/Hunting.cs`:
  - `StrikeAnimal(animalId)`: needs a Piercing (spear) or Cutting item. Spear does 50 + q/4 damage (2 strikes), blade 30 + q/8 (about 3). Reach is World.Reach + 0.7 (spear) or + 0.2 (blade), + 1 m on a running target. One strike per animal per second. A struck sheep leaps 1.4 m away and bolts; wounded sheep run slower (`AnimalMove`), so a kill is a short chase. Wounds heal 2 points/s from 10 s after the last strike once it stops running. Reports `animal.kill`.
  - Death inserts a public `animal_carcass` row (position, heading, fleece, growth, former animal id for its fleece color) and removes the animal; nearby flock-mates take fright and leadership passes on. Unbutchered carcasses go after one in-game day.
  - `ButcherCarcass(id)`: needs a blade; quality = min(blade, QualityCapFor "animal.butcher"). Drops 3 Raw Mutton (190) + 1 Sheep Hide (194) as world items, or 1 mutton for a lamb (growth < 80).
  - Lambs: `Flock.Size` (set at spawn; old flocks count themselves on the first tick) and `Flock.LastBirthAt`. A flock below size gains one animal per in-game day: a lamb (growth 35, grows to 100 over 2 days) beside an adult if 2+ adults remain, else a stray adult at home (default chosen for D-039's "minimum of 2"; PM told).
  - Schema is additive: new columns have `[Default]`, new table.
- Client `client/scripts/animals/`:
  - `Strike.cs`: shared strike-target registry (like Use for G). Lanes register `(at, facing) => StrikeOffer?`; the nearest offer wins. Claims adds players there (claims-3).
  - `Herd`: strike on action "strike" (UX binds LMB / Z in Controls.cs; Herd only adds it if missing), ignored while crafting or placing a building (`herd.Blocked` hook in Main). G Use handlers: "Shear" (15) and "Butcher" (16), both only when carrying a blade. Requests the "strike" hint (UX adds the row) every 30 s at most when carrying a spear near animals.
  - `Sheep`: lambs scale to about half size and grow; `LieDown` settles a dying sheep onto its side with legs tucked (the same node, if it was alive on screen); `FadeAway` sinks it after butchering.

## Progress log
- 2026-10-15 23:25: server and client written; local hunt: 2 spear strikes kill, butcher drops 3 mutton (q35, novice cap) and 1 hide. Lamb regrowth verified with a temporary 8 s timer.
- 2026-10-15 23:55: screenshots screenshots/animals-04-*.png (fallen sheep, lambs, flock gathering).

- 2026-10-16 00:10: merged main 58d50e0 (via #23 branch, Kiln fix ported); added Strike registry for Claims; herd.Facing hook in Main.

## Open questions / blockers
- Needs #20 (Item.RawMutton) on main before this PR can build there; the local branch merged crafting-cook to build.
- UX lane to add an OnRequest "strike" hint row (asked).

## Next step
Wait for CI on #23, send the green sha to the PM (session cse_01HQQLrCgy3JhRUA9YMTqyL1).
