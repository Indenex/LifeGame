# Admin

Who may use the admin item panel (F10) and the time commands. The list is private on the server: a game client sees only whether it is itself on it.

## First admin (one command)

The database owner (whoever ran `spacetime publish`) is always an admin on the server, but a game client signs in with its own identity. Log in once as a player, note the character's name, then from the machine that published the database:

```
spacetime call <database> make_admin '"Character Name"'
```

That player is now an admin. Admins can add others the same way (`make_admin` needs an admin). There is no way to add an admin from a game client without being one.

## What an admin can do

- F10 opens the item panel: search every item, set a quantity (and a quality for made things) and press Give. The items go into the admin's own bag (`admin_give`, logged on the server with the admin's identity).
- The time and season commands (`/time`, `/season`, `/season-speed`).
