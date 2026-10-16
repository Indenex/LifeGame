# KEYMAP.md

The one list of every key the game uses. Before binding a key, add it here on your branch; if the key is taken, ask the PM. Keys that only work inside an open panel or minigame are listed under that context and may reuse a key from the world list.

Status: DECIDED by Justin on 2026-10-15 (D-025). The UX/UI lane owns this file, the input map and the shared Use key (D-033); each lane moves its keys to this table after that lands.

The keys themselves live in `client/scripts/ui/Controls.cs`, which binds them before any lane does. There is no controls line (D-038): players learn keys from the G prompt, hints inside panels, and a one-time hint per key that `Controls.cs` also holds (shown once per install, skipped if the player finds the key first). Don't add key lists to the HUD; ask the UX/UI lane for a hint instead. Holding F1 shows every world key, built from that table and the live input map. Give your action a row there (with its help text) so it appears. To add or change a world key, change that table and this file together (ask the UX/UI lane, or the PM if the key is taken).

## In the world

| Key | Action | Lane | Today |
|---|---|---|---|
| W A S D / arrows | Walk | Crafting | same |
| Q / E | Turn | Crafting | same |
| Hold right mouse + drag | Turn the view freely, eased (D-050) | UX/UI | new |
| Hold Shift | Run (uses stamina). Bound now; out of the F1 list until the stamina lane makes it work | whichever lane builds stamina; reads the `run` action | new, D-088 |
| Hold Ctrl | Sneak (slow walk; sheep stay calm). Was Shift until D-088 | Animals (small hook in Main.cs) | D-026, moved to Ctrl by D-091 |
| Space | Pick up | Crafting | same |
| F | Dig | Crafting | same |
| G | **Use** what you're facing: pick berries, feed a fire, drink at water or from your pot, add materials to a blueprint, hang, open or shut a door, open a basket, read a note, tend a claim marker | all | G forage (Survival), G feed fire (Crafting), G drink (Survival, R gone), H add materials (Building), R read note (Journal) |
| Shift+G | Bar or unbar a hut door (whoever hung it, or claim members); read inside Building's door offer, no separate binding | Building | new, building-3; confirmed by UX/UI 2026-10-16 (also in the F1 list as "Shift G") |
| C | Crafting panel | Crafting | C also eat (Survival) |
| X | Eat the best food you carry | Survival | C |
| B | Build menu (huts and the claim marker) | Building | B also place claim (Claims) |
| K | Claim panel | Claims | T |
| J | Journal | Journal | same |
| Enter | Chat | Claims | same |
| M | Music on/off | Atmosphere | same |
| Left click / Z | Strike (attack an animal, D-039) | Animals | new |
| Hold Ctrl while starting a craft | Work it by hand: a Master plays the minigame anyway (D-047). Shares Ctrl with sneak; it only counts in the crafting panel (D-091) | Journal and skills | new |
| Hold F1 | Show every world key | UX/UI | new |
| Esc | Menu (resume, keys, volume, music, quit) when no panel, chat or placement is open. A panel that closes on Esc must call `GetViewport().SetInputAsHandled()` | UX/UI | new |

## Inside a panel or mode

| Context | Keys |
|---|---|
| Crafting panel | Tab next tier, 1-9 pick a row, Esc close |
| Build placement | T / Shift+T rotate, Tab next building, Enter place, Esc cancel |
| Minigames (knapping, shaping) | Space, A/D, T (ceramics), Esc quit |
| Basket panel | Click moves one item, Shift+click moves all, G or Esc close |


## Chat commands

Typed into chat (Enter). Admin commands show in F1 only for admins.

| Command | Action | Who | Lane |
|---|---|---|---|
| /name Name | Change your name | everyone | Claims |
| /day | Set the time to mid-morning; the light eases over a second or two | admin | Atmosphere |
| /night | Set the time to night | admin | Atmosphere |
| /time 0-1 | Set the time of day (0.25 sunrise, 0.5 noon, 0.75 sunset) | admin | Atmosphere |
| /season name | Jump to spring, summer, autumn or winter (or 0-1 through the year) | admin | Atmosphere |
| /season-speed days | In-game days each season lasts (1 by default) | admin | Atmosphere |

The publisher is always an admin. To make your in-game player an admin, run once from the machine that published: `spacetime call lifegame make_admin '"YourName"'`.

## Debug only (not in player builds)

| Key | Action | Lane | Today |
|---|---|---|---|
| F5 | Hold to fast-forward time | Atmosphere | T |
| F6 | Cycle weather | Atmosphere | Y |

## How to add a Use action (G)

Don't bind G. Register a handler from your own lane's files (once, e.g. in `_Ready` or after you get the connection); the Use key shows its prompt and runs it.

```csharp
Use.Register(new UseHandler
{
    Id = "survival.forage",   // lane.action; registering the same Id again replaces it
    Priority = 10,            // higher wins when several things are in reach, then the nearest
    Find = at =>              // the player's position; return null when there's nothing to do
    {
        if (NearestRipeBush() is not { } bush) return null;
        float d = new Vector2(bush.X - at.X, bush.Z - at.Z).Length();
        return new UseOffer("Pick berries", d, () => conn!.Reducers.Forage(bush.Id),
            Target: new Vector3(bush.X, 0, bush.Z));
    },
});
```

- The prompt is a short verb phrase with a capital first letter: "Pick berries", "Drink", "Feed the fire".
- Return an offer only when G would actually do something (e.g. feed fire needs fuel in hand), so the prompt never promises what G can't do.
- Pass `Target` (where the thing is) so G picks what the player faces: an offer with a Target within 60° of the player's facing beats one with no Target, which beats one with a Target off to the side; priority and distance decide within that. So a bush in front of you wins over a fire beside you, without a special priority.
- Hold actions: pass `Repeat` seconds, e.g. `new UseOffer("Add materials", d, AddOne, Repeat: 0.65f)`; it runs on press and again every 0.65 s while G is held.
- G is ignored while the crafting minigame runs or a building is being placed.
- Priorities in use: Building add materials 30, Building doors and baskets (hang, open, shut a door; open a basket) 28, Ceramics load or fire a kiln 26, Crafting take meat off the fire 25, Survival forage_facing 25 (a stopgap until forage passes a Target), Ceramics set pots in a fire or fire a dry pot beside one 22, Crafting feed fire 20, Animals shear 15, Ceramics pick up a dry pot or brick 12, Ceramics fill pots 11, Ceramics pick up a fired pot, sherd or dry brick on the ground 10, Survival forage and drink 10, Journal read note 5, Survival drink from your pot 1 (only when thirsty, away from water, carrying a pot of water). A Target you face beats all of these.
- A hint that should appear at a moment rather than in the opening sequence (e.g. sneak, when a flock bolts near you) is an `OnRequest` row in `Controls.cs`; the lane calls `Hints.Request("sneak")` when the moment comes. It shows once, and never if the player already used the key. Ask the UX/UI lane for a new slot.
- Players can change any key on the Controls page of the Esc menu (ui/ControlsPage.cs); the keys in this file are the defaults. So never write a key into on-screen text: hints use `{key}` in Controls.cs, and lanes call `Controls.KeyFor("journal")` for prompts like "J to close". Read keys through input actions (`IsActionPressed`), not raw keycodes, or the player's choice won't reach you.
- A one-off tip that isn't about a key: `Hints.Tip("ceramics.dry_by_fire", "Clay dries twice as fast beside a burning fire")` at the moment it matters. It shows once per install, in the same corner. Keep it to one short sentence.
- If the player does a hinted thing without its key (the server saw them pick berries, eat, open a panel by clicking), call `Hints.Done("eat")` so the hint is never taught after the fact. S, A and D count as walking; E and a right-drag count as turning.
- A full panel (journal, claim panel, Esc menu) hides the world HUD while it is open. `Panels.Open()` (client/scripts/ui/Panels.cs) says whether one is; HUD pieces outside the UX/UI lane (the land chip, the needs rings) fade out while it is true. A new full panel is added to `Panels.Open` in Main. A side panel that leaves the world in view (crafting) goes in `Panels.Side` instead: the G prompt steps aside, the inventory row stays.
- After migrating, remove your old key from your lane's `Bind` and from your own prompt text.
