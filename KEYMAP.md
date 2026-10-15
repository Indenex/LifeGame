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
| Hold Shift | Sneak (slow walk; sheep stay calm) | Animals (small hook in Main.cs) | new, D-026 |
| Space | Pick up | Crafting | same |
| F | Dig | Crafting | same |
| G | **Use** what you're facing: pick berries, feed a fire, drink at water or from your pot, add materials to a blueprint, read a note, tend a claim marker | all | G forage (Survival), G feed fire (Crafting), G drink (Survival, R gone), H add materials (Building), R read note (Journal) |
| C | Crafting panel | Crafting | C also eat (Survival) |
| X | Eat the best food you carry | Survival | C |
| B | Build menu (huts and the claim marker) | Building | B also place claim (Claims) |
| K | Claim panel | Claims | T |
| J | Journal | Journal | same |
| Enter | Chat | Claims | same |
| M | Music on/off | Atmosphere | same |
| Left click / Z | Strike (attack an animal, D-039) | Animals | new |
| Hold Ctrl while starting a craft | Work it by hand: a Master plays the minigame anyway (D-047) | Journal and skills | new |
| Hold F1 | Show every world key | UX/UI | new |
| Esc | Menu (resume, keys, volume, music, quit) when no panel, chat or placement is open. A panel that closes on Esc must call `GetViewport().SetInputAsHandled()` | UX/UI | new |

## Inside a panel or mode

| Context | Keys |
|---|---|
| Crafting panel | Tab next tier, 1-9 pick a row, Esc close |
| Build placement | T / Shift+T rotate, Tab next building, Enter place, Esc cancel |
| Minigames (knapping, shaping) | Space, A/D, T (ceramics), Esc quit |

Interim until crafting-2 (Use key) lands: tend clay is V (Ceramics); it then moves to G.

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
        return new UseOffer("Pick berries", d, () => conn!.Reducers.Forage(bush.Id));
    },
});
```

- The prompt is a short verb phrase with a capital first letter: "Pick berries", "Drink", "Feed the fire".
- Return an offer only when G would actually do something (e.g. feed fire needs fuel in hand), so the prompt never promises what G can't do.
- Hold actions: pass `Repeat` seconds, e.g. `new UseOffer("Add materials", d, AddOne, Repeat: 0.65f)`; it runs on press and again every 0.65 s while G is held.
- G is ignored while the crafting minigame runs or a building is being placed.
- Priorities in use: Building add materials 30, Crafting take meat off the fire 25, Crafting feed fire 20, Animals shear 15, Survival forage and drink 10, Journal read note 5, Survival drink from your pot 1 (only when thirsty, away from water, carrying a pot of water).
- A hint that should appear at a moment rather than in the opening sequence (e.g. sneak, when a flock bolts near you) is an `OnRequest` row in `Controls.cs`; the lane calls `Hints.Request("sneak")` when the moment comes. It shows once, and never if the player already used the key. Ask the UX/UI lane for a new slot.
- After migrating, remove your old key from your lane's `Bind` and from your own prompt text.
