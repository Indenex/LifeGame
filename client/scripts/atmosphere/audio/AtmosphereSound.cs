using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// What Atmosphere senses for the sound (audio-2, Audio lane): which surface is
// underfoot, where the water is, and the season handed to the mixer.
public partial class Atmosphere
{
    float stepPhase;
    Vector3 lastPlayerPos;

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
        if (distance > 2f || distance < 0.0005f) return;
        float before = stepPhase;
        stepPhase += distance * 2.4f;
        if (Mathf.Floor(before / Mathf.Pi + 0.5f) == Mathf.Floor(stepPhase / Mathf.Pi + 0.5f)) return;

        sound.Step(GroundAt(pos), wetness);
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
}
