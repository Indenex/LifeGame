using SpacetimeDB.Types;

namespace LifeGame;

// The Audio lane's hooks into Atmosphere, in one place so Atmosphere.cs has a single line for them
// (Connect once, Tick every frame) and new sounds never touch it again. Add new sound
// schedulers here.
static class AudioHooks
{
    public static void Connect(DbConnection conn)
    {
        ToolSounds.Connect(conn);
        RoastSounds.Connect(conn);
        StorageSounds.Connect(conn);
        StructureSounds.Connect(conn);
        PotSounds.Connect(conn);
        RestSounds.Connect(conn);
        EmoteSounds.Connect(conn);
        LampSounds.Connect(conn);
    }

    public static void Tick(double dt)
    {
        ToolSounds.Tick(dt);
        RoastSounds.Tick(dt);
        PotSounds.Tick(dt);
        EmoteSounds.Tick(dt);
        LampSounds.Tick(dt);
    }
}
