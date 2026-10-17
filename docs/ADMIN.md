# Admin

Who may use the admin item panel (F10) and the time commands. The list is private on the server: a game client sees only whether it is itself on it.

## First admin (one command)

The database owner (whoever ran `spacetime publish`) is always an admin on the server, but a game client signs in with its own identity. Log in once as a player, note the character's name, then from the machine that published the database:

```
spacetime call <database> make_admin '"Character Name"'
```

That player is now an admin. Admins can add others the same way (`make_admin` needs an admin). There is no way to add an admin from a game client without being one.

## What an admin can do (chat commands)

- `/give` opens the item menu: search every item, set a quantity (and a quality for made things) and press Give. `/give <item> [n]` gives straight away, e.g. `/give stone knife 3`. The items go into the admin's own bag (`admin_give`, logged on the server with the admin's identity).
- `/fly` toggles flight: very fast free movement in 3D, no gravity or collision (Space rises, Ctrl sinks). The server skips the walking pace only for an admin who is flying, and checks admin again on every move.
- `/time hh:mm` (06:30), `/time dawn|noon|dusk|night`, or `/time 0-1`; `/day`, `/night`.
- `/season spring|summer|autumn|winter` (or 0 to 1 through the year); `/season-speed days`.
- Every one is checked by the server; a player who isn't an admin gets "Only the world's admin can...".
