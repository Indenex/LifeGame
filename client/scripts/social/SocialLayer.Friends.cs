using System.Linq;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-32: the friends panel ("/friends") and a quiet note when a friend comes online.
public partial class SocialLayer
{
    readonly FriendsPanel friends = new();
    readonly FriendWay way = new();

    public void ToggleFriends() => friends.Toggle();

    // ui-84: the group list shows the way to a member with the same arrow.
    public void ShowWay(uint id, string name, System.Func<Godot.Vector2?> at) => way.Follow(id, name, at);
    public uint ShowingWay => way.Target;

    void ReadyFriends()
    {
        AddChild(friends);
        AddChild(way);
    }

    void AttachFriends(DbConnection c)
    {
        friends.Conn = c;
        friends.MyId = () => myId;
        friends.MyPos = () => myPos;
        friends.Note = text => hud.Note(text);
        friends.ShowWay = way.Follow;
        friends.Showing = () => way.Target;
        way.MyPos = () => myPos;
        way.Note = text => hud.Note(text);
        way.NameOf = id => c.Db.Player.PlayerId.Find(id)?.Name ?? "Your friend";
        way.Spot = id => c.Db.MyFriendSpots.Iter().FirstOrDefault(s => s.PlayerId == id) is { Online: true } s ? new Godot.Vector2(s.X, s.Z) : null;
        c.Db.MyFriendSpots.OnUpdate += (_, was, now) =>
        {
            if (was.Online || !now.Online) return;
            string name = c.Db.Player.PlayerId.Find(now.PlayerId)?.Name ?? "A friend";
            hud.Note($"{name} is here");
        };
    }
}
