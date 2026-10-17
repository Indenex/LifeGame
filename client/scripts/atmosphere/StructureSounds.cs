using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// Buildings heard (audio-16): a door opening, shutting, being barred or unbarred, as CueDefs
// played where the building stands (Sfx.PlayAnimal: full volume beside it, fading out to 25 m).
static class StructureSounds
{
    const float Hearing = 25f;

    public static void Connect(DbConnection c)
    {
        c.Db.StructureDoor.OnUpdate += (_, was, d) =>
        {
            CueDef? def = null;
            if (was.Locked != d.Locked) def = d.Locked ? CueDefs.DoorBar : CueDefs.DoorUnbar;
            else if (was.Closed != d.Closed) def = d.Closed ? CueDefs.DoorShut : CueDefs.DoorOpen;
            if (def == null || c.Db.Structure.Id.Find(d.StructureId) is not { } s) return;
            Sfx.PlayAnimal(def, new Vector3(s.X, 0f, s.Z), Hearing);
        };
    }
}
