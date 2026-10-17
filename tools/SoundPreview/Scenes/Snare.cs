using LifeGame;

static partial class AudioScenes
{
    // Snare scene: set at 1 s, a catch at 3 s, collected at 5 s.
    [Scene("snare")]
    static void Snare(Block s)
    {
        var mix = s.Mix;
        bool At(double x) => s.At(x);
        if (At(1.0)) mix.Play(CueDefs.SnareSet);
        if (At(3.0)) mix.Play(CueDefs.SnareSnap);
        if (At(5.0)) mix.Play(CueDefs.SnareCollect);
    }
}
