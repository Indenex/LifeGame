using SpacetimeDB.Types;

namespace LifeGame;

// claims-32: the friends panel ("/friends") and a quiet note when a friend comes online.
public partial class SocialLayer
{
    readonly FriendsPanel friends = new();

    public void ToggleFriends() => friends.Toggle();

    void ReadyFriends() => AddChild(friends);

    void AttachFriends(DbConnection c)
    {
        friends.Conn = c;
        friends.MyId = () => myId;
        friends.MyPos = () => myPos;
        friends.Note = text => hud.Note(text);
        c.Db.MyFriendSpots.OnUpdate += (_, was, now) =>
        {
            if (was.Online || !now.Online) return;
            string name = c.Db.Player.PlayerId.Find(now.PlayerId)?.Name ?? "A friend";
            hud.Note($"{name} is here");
        };
    }
}
