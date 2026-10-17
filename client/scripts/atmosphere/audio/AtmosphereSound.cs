using System.Linq;
using Godot;
using SpacetimeDB.Types;
using LifeGame.Shared;

namespace LifeGame;

// What Atmosphere senses for the sound (audio-2, Audio lane): which surface is
// underfoot, where the water is, and the season handed to the mixer.
public partial class Atmosphere
{
    float stepPhase;
    Vector3 lastPlayerPos;
    float lastWaterDepth, lapTimer;

    // One step per stride, matching the avatar's leg swing.
    void Footsteps()
    {
        sound.Year = Year;
        sound.SnowCover = SnowCover;
        if (Player == null || !Player.IsInsideTree()) return;
        var pos = Player.GlobalPosition;
        var flat = pos - lastPlayerPos;
        flat.Y = 0;
        lastPlayerPos = pos;
        float distance = flat.Length();
        float depth = Wading.Depth(pos.Y);
        WaterBody(pos, depth, distance);
        lastWaterDepth = depth;
        if (distance > 2f || distance < 0.0005f) return;
        float before = stepPhase;
        stepPhase += distance * 2.4f;
        if (Mathf.Floor(before / Mathf.Pi + 0.5f) == Mathf.Floor(stepPhase / Mathf.Pi + 0.5f)) return;

        if (depth > 0.05f && Sheltered?.Invoke(pos) != true) WadeStep(depth);
        else sound.Step(GroundAt(pos), wetness);
    }

    // One stride in water: a splash that gets deeper with the water, or a paddle stroke when afloat.
    void WadeStep(float depth)
    {
        lapTimer = 0f;
        sound.Play(depth >= Wading.FloatDepth ? CueDefs.Paddle : depth > 0.65f ? CueDefs.WadeDeep : depth > 0.3f ? CueDefs.WadeKnee : CueDefs.WadeShallow);
    }

    // Going in (a splash by how fast), coming out (dripping), and the quiet lapping while afloat and still.
    void WaterBody(Vector3 pos, float depth, float distance)
    {
        if (Sheltered?.Invoke(pos) == true) return;
        float dt = (float)GetProcessDeltaTime();
        if (lastWaterDepth <= 0.05f && depth > 0.05f)
        {
            float speed = dt > 0f ? Mathf.Min(distance, 2f) / dt : 0f;
            Sfx.Splash(pos, Mathf.Clamp(speed / 6f, 0.15f, 1f));
        }
        else if (lastWaterDepth > 0.05f && depth <= 0.05f) sound.Play(CueDefs.Drip);
        if (depth >= Wading.FloatDepth && distance < 0.0005f)
        {
            lapTimer += dt;
            if (lapTimer > 2.6f) { lapTimer = 0f; sound.Play(CueDefs.Lap); }
        }
    }

    // What is underfoot at a position: used by footsteps and by landings (Sfx.Land).
    public Ground GroundAt(Vector3 pos)
    {
        int cx = World.CellOf(pos.X), cz = World.CellOf(pos.Z);
        terrain ??= GetParent()?.GetChildren().OfType<Terrain>().FirstOrDefault();
        return Sheltered?.Invoke(pos) == true ? Ground.Floor
            : pos.Y < Look.WaterLevel + 0.05f ? Ground.Water
            : SnowCover > 0.3f ? Ground.Snow
            : NearWater(cx, cz) ? Ground.Sand
            : World.IsClay(cx, cz) ? Ground.Clay
            : terrain?.Cleared?.Invoke(cx, cz) == true ? Ground.Earth
            : Ground.Grass;
    }

    // Sand lies at the banks: any cell within about 3 m is below the water line.
    bool NearWater(int cx, int cz)
    {
        if (terrain == null) return false;
        for (int dx = -3; dx <= 3; dx += 3)
            for (int dz = -3; dz <= 3; dz += 3)
                if (terrain.TileHeight(cx + dx, cz + dz) < Look.WaterLevel - 0.05f) return true;
        return false;
    }

    // Where the nearest open water is, looked over a few times a second: the closer
    // and the more of it, the louder it sounds; the sound comes from its side.
    // Water in a stream's channel (Land.StreamDistance < 4) runs; the rest laps.
    float waterLook;

    const float StreamWidth = 4f;

    void WaterSound(float dt, Vector3 focus, Vector3 right)
    {
        waterLook -= dt;
        if (waterLook > 0f || terrain == null) return;
        waterLook = 0.25f;
        const int Reach = 20, Step = 2;
        int cx = World.CellOf(focus.X), cz = World.CellOf(focus.Z);
        var pond = new WaterSpot();
        var stream = new WaterSpot();
        int near = 0;
        for (int dx = -Reach; dx <= Reach; dx += Step)
            for (int dz = -Reach; dz <= Reach; dz += Step)
            {
                float d2 = dx * dx + dz * dz;
                if (d2 > Reach * Reach) continue;
                if (d2 <= 10 * 10) near++;
                if (terrain.TileHeight(cx + dx, cz + dz) >= Look.WaterLevel - 0.05f) continue;
                var spot = Land.StreamDistance(cx + dx, cz + dz) < StreamWidth ? stream : pond;
                spot.Add(dx, dz, d2);
            }
        var r = new Vector2(right.X, right.Z);
        sound.Water = pond.Amount(near, Reach);
        sound.WaterPan = pond.Pan(r);
        sound.Stream = stream.Amount(near, Reach);
        sound.StreamPan = stream.Pan(r);
    }

    sealed class WaterSpot
    {
        float nearest = float.MaxValue;
        Vector2 toward;
        int wet;

        public void Add(int dx, int dz, float d2)
        {
            nearest = Mathf.Min(nearest, Mathf.Sqrt(d2));
            if (d2 <= 10 * 10) wet++;
            toward += new Vector2(dx, dz) / (1f + d2 * 0.05f);
        }

        // A puddle-sized dip sounds quietly; the shore of a wide water fully.
        public float Amount(int near, int reach)
        {
            if (nearest == float.MaxValue) return 0f;
            float amount = Mathf.Clamp(wet / (float)near * 3f, 0.35f, 1f);
            return (1f - Mathf.SmoothStep(1.5f, reach, nearest)) * amount;
        }

        public float Pan(Vector2 right) =>
            toward.LengthSquared() > 0.01f ? Mathf.Clamp(toward.Normalized().Dot(right), -1f, 1f) : 0f;
    }

    // Each species goes to the voices its Def names (AnimalDef.Voice: sheep, deer, hen; the rabbit's
    // are in RabbitSounds, which reads the same id). A new species with an existing voice is a Def line.
    void HearAnimal(Animal a)
    {
        bool running = a.State == AnimalState.Run;
        switch (AnimalDefs.Get((byte)a.Species).Voice)
        {
            case "sheep":
                Sheep.Seen(a.Id, a.X, a.Z, running, a.Growth < 100);
                break;
            case "deer":
                Deer.Seen(a.Id, a.X, a.Z, running, global::LifeGame.Sheep.IsStag(a.Id));
                break;
            case "hen":
                Hens.Seen(a.Id, a.X, a.Z, a.State switch { AnimalState.Graze => HenVoices.Mood.Peck, AnimalState.Walk => HenVoices.Mood.Walk, AnimalState.Run => HenVoices.Mood.Run, AnimalState.Roost => HenVoices.Mood.Roost, _ => HenVoices.Mood.Idle });
                break;
        }
    }

    // How much it is stag-roaring weather, 0 to 1: dusk in autumn (the year runs 0 spring,
    // 0.25 summer, 0.5 autumn, 0.75 winter; sunset is at 0.75 of the day).
    float StagRoarTime()
    {
        float autumn = Mathf.SmoothStep(0.5f, 0.53f, Year) * (1f - Mathf.SmoothStep(0.72f, 0.75f, Year));
        float t = timeOfDay < 0f ? 0.5f : timeOfDay;
        float dusk = Mathf.SmoothStep(0.66f, 0.74f, t) * (1f - Mathf.SmoothStep(0.82f, 0.9f, t));
        return autumn * dusk;
    }

    // A blow on an animal: a thud, or a crack for a sneak-attack crit. An arrow's plain hit
    // is voiced by its ArrowShot (the thunk), so only a crit adds the crack.
    void HearHit(AnimalHit h)
    {
        if (h.Ranged && !h.Crit) return;
        Sfx.PlayHit(h.Crit ? Cue.Crit : Cue.Hit, new Vector3(h.X, 0f, h.Z), new Vector3(h.FromX, 0f, h.FromZ));
    }

    const float ArrowSpeed = 60f; // metres a second: how long until the thunk is heard

    // The twang and whoosh where the arrow leaves (full volume if it is you), then, when it
    // lands, a thunk into the animal, a tree (in woods) or the ground.
    void HearArrow(ArrowShot shot)
    {
        var from = new Vector3(shot.FromX, 0f, shot.FromZ);
        var to = new Vector3(shot.ToX, 0f, shot.ToZ);
        Sfx.PlayShot(from, to, shot.Hit
            ? Cue.ArrowFlesh
            : Forest.Density(World.CellOf(shot.ToX), World.CellOf(shot.ToZ)) > 0.45f ? Cue.ArrowWood : Cue.ArrowEarth,
            (from - to).Length() / ArrowSpeed, GetTree());
    }

    // Sounds for the quick body actions (player_act): data cues (CueDefs) timed to the motion,
    // played from where that player stands, so nearby players hear each other.
    static CueDef? SoundOfAct(WorkMotion motion, ushort item) => motion switch
    {
        WorkMotion.Drink => CueDefs.Drink,
        WorkMotion.Eat => CueDefs.Eat,
        WorkMotion.Pluck => CueDefs.Pluck,
        WorkMotion.PickUp => CueDefs.PickUp,
        _ => null,
    };

    void HearAct(DbConnection conn, PlayerAct act)
    {
        if (SoundOfAct((WorkMotion)act.Motion, act.Item) is not { } def) return;
        if (conn.Db.PlayerPos.PlayerId.Find(act.PlayerId) is not { } at) return;
        Sfx.PlayBy(def, new Vector3(at.X, 0f, at.Z));
    }

    // How much each fire can hold ahead of now (its lighter's Firecraft and a hearth's scale, set by
    // the server on the fire row: FuelCapSeconds), so a well-stoked fire is judged against its own cap.
    readonly System.Collections.Generic.Dictionary<ulong, float> fireCaps = new();

    void HearFireCaps(DbConnection conn)
    {
        conn.Db.Fire.OnInsert += (_, f) => fireCaps[f.Id] = f.FuelCapSeconds;
        conn.Db.Fire.OnUpdate += (_, _, f) => fireCaps[f.Id] = f.FuelCapSeconds;
        conn.Db.Fire.OnDelete += (_, f) => fireCaps.Remove(f.Id);
    }

    // 0 to 1: how stoked a fire is, rising from 70% to 80% of its own cap (the same ramp as the
    // smoke signal, so the roar and the plume come together). Fires from before the column
    // (cap 0) are judged against the Master cap.
    float Stoke(ulong id, float left)
    {
        float cap = fireCaps.TryGetValue(id, out var c) && c > 0f ? c : Fires.MasterMaxSeconds;
        return Mathf.SmoothStep(0.70f, 0.80f, left / cap);
    }

    // Kilns that are firing, with when they finish (microseconds since the epoch), for the roar.
    readonly System.Collections.Generic.Dictionary<ulong, (float X, float Z, long Finish)> kilnFires = new();

    void HearKilns(DbConnection conn)
    {
        void Set(Kiln k)
        {
            if (k.State == 1) kilnFires[k.StructureId] = (k.X, k.Z, k.FireFinish.MicrosecondsSinceUnixEpoch);
            else kilnFires.Remove(k.StructureId);
        }
        conn.Db.Kiln.OnInsert += (_, k) => Set(k);
        conn.Db.Kiln.OnUpdate += (_, _, k) => Set(k);
        conn.Db.Kiln.OnDelete += (_, k) => kilnFires.Remove(k.StructureId);
    }
}
