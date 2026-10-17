using Godot;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LifeGame;

// "I'm stuck" (bugfix-44): the Esc menu entry and /stuck ask the server (unstuck), which moves
// you to the nearest free spot. The server also says no (cooldown, mid-air, not actually stuck).
public partial class Main
{
    void HookUnstuck(DbConnection c)
    {
        StuckCommand.Request = () => c.Reducers.Unstuck();
        c.Reducers.OnUnstuck += ctx =>
        {
            switch (ctx.Event.Status)
            {
                case Status.Committed:
                    // The server moved us; a short hop is under the usual snap-back distance, so go there now.
                    if (conn?.Db.PlayerPos.PlayerId.Find(myId) is { } p)
                        me.Position = new Vector3(p.X, Wading.FeetY(terrain.HeightAt(p.X, p.Z)), p.Z);
                    hud.Note("You shake yourself free.");
                    break;
                case Status.Failed(var reason):
                    hud.Note(reason);
                    break;
            }
        };
    }

    // The Esc menu's "I'm stuck" entry calls this.
    public void RequestUnstuck()
    {
        if (conn == null) hud.Note("You aren't connected to a world");
        else StuckCommand.Request?.Invoke();
    }
}
