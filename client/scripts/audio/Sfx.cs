namespace LifeGame;

// One-line entry point for feedback sounds from any lane (audio-3):
//   Sfx.Play(Cue.Eat);
// Safe to call before the soundscape exists or from any thread; the sound is
// queued for the synth thread. Ask the Audio lane for a new Cue rather than
// making sounds elsewhere (D-079).
public static class Sfx
{
    static SoundScape? scape;

    internal static void Attach(SoundScape? soundScape) => scape = soundScape;

    public static void Play(Cue cue) => scape?.Play(cue);

    // Touching down after a jump: strength 0 (a hop) to 1 (a long fall), and the ground underfoot.
    public static void Land(float strength, Ground ground) => scape?.Land(ground, strength);
}
