using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// A home friends built together (building-27, Justin's "names plus a house-warming"): when
// it finishes, everyone who helped gets a note naming the others, and the helpers nearby
// hear a quiet cheer. Nothing changes in play; it reads the public structure_credit rows.
public partial class BuildingSystem
{
    const float WarmingHearing = 25f; // metres; helpers farther off only get the note

    // Shelters people live in: the lean-to and the kinds with a doorway (huts and houses).
    static bool ForPeople(int kind) => kind == (int)StructureKind.LeanTo || Structures.IsHome(kind);

    void Finished(Structure s, Vector3 at, float distance)
    {
        string name = ShapeOf(s).Name.ToLowerInvariant();
        var credits = Credits(s.Id);
        var others = credits.Where(x => !x.Me).Select(x => x.Name).ToList();
        if (!ForPeople((int)s.Kind) || others.Count == 0 || !credits.Any(x => x.Me))
        {
            if (distance < 30f) hud.Note($"The {name} is finished");
            return;
        }
        string with = others.Count == 1 ? others[0] : string.Join(", ", others.Take(others.Count - 1)) + " and " + others[^1];
        hud.Note($"You and {with} finished the {name}. Welcome home!");
        if (distance < WarmingHearing)
            Sfx.Play(CueDefs.HouseWarming, Mathf.Pow(1f - distance / WarmingHearing, 1.2f));
    }

    // --building-housewarming (with --building-showcase): the note and the cheer as a helper
    // standing by sees and hears them, for screenshots without two players.
    void PreviewWarming() => GetTree().CreateTimer(3).Timeout += () =>
    {
        hud.Note("You and Ana finished the log cabin. Welcome home!");
        Sfx.Play(CueDefs.HouseWarming);
    };
}
