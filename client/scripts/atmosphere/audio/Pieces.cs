namespace LifeGame;

// The game's music, as data (audio-8): pieces (Track) and the styles that play them (Arrangement).
// A new piece or a new mood is a new entry here; Score plays them.
public static class Pieces
{
    // Main theme, "Kites" mood: airy and buoyant, in G major, 6/8. An original tune.
    public static readonly Track MainTheme = new()
    {
        Name = "theme",
        RootMidi = 55, // G3; the melody sits an octave above
        Bpm = 84f,
        Bars = 16,
        Melody = Track.ParseMelody(
            "5:2 1':1 3':3 | 2':2 1':1 7:3 | 6:2 1':1 5:3 | 4:2 3:1 2:3 | " +
            "5:2 1':1 3':3 | 5':2 4':1 3':3 | 2':2 3':1 2':1 1':1 7:1 | 1':4 r:2 | " +
            "3':2 5':1 3':1 2':1 1':1 | 7:2 2':1 5:3 | 6:2 1':1 6:1 5:1 3:1 | 4:2 6:1 5:3 | " +
            "1':2 3':1 5':3 | 6':2 5':1 3':3 | 2':2 1':1 7:1 5:1 7:1 | 1':4 r:2"),
        Chords = Track.ParseChords("I V vi IV I IV V I  I V vi IV I IV V I"),
    };

    // The nap song: a slow lullaby in D major, 6/8. Also original.
    public static readonly Track NapSong = new()
    {
        Name = "nap",
        RootMidi = 50, // D3
        Bpm = 56f,
        Bars = 16,
        Melody = Track.ParseMelody(
            "5:3 3:2 1:1 | 2:3 3:3 | 5:3 6:2 5:1 | 3:6 | " +
            "5:3 3:2 1:1 | 2:3 4:3 | 3:2 2:2 1:2 | 1:6 | " +
            "1':3 6:2 5:1 | 6:3 5:3 | 3:2 5:2 3:2 | 2:6 | " +
            "1':3 6:2 5:1 | 4:3 3:3 | 2:2 3:2 2:2 | 1:6"),
        Chords = Track.ParseChords("I V IV I I V I I  I vi IV V I IV V I"),
    };

    // A "Sunny picnic": plucked harp, recorder, bells, a light shaker and woodblock.
    public static readonly Arrangement Sunny = new()
    {
        Name = "A-sunny-picnic",
        Melody = Inst.Flute, MelodyGain = 0.9f,
        Double = Inst.Bell, DoubleGain = 0.22f,
        Arp = Inst.Pluck, ArpGain = 0.34f, ArpPattern = new[] { 0, 1, 2, 3, 2, 1 }, ArpOctave = 0,
        Bass = Inst.Triangle, BassGain = 0.5f, BassPattern = new[] { 0, -1, -1, 2, -1, -1 },
        PadOn = true, PadGain = 0.16f,
        Tick = Inst.Shaker, TickGain = 0.11f,
        Accent = Inst.Wood, AccentGain = 0.12f,
        Reverb = 0.3f,
    };

    // "Music-box storybook": slower, music box and bells, a soft pad, no percussion.
    public static readonly Arrangement MusicBoxStyle = new()
    {
        Name = "B-music-box",
        TempoScale = 0.84f,
        Melody = Inst.MusicBox, MelodyGain = 0.8f,
        Double = Inst.Bell, DoubleGain = 0.16f,
        Arp = Inst.Pluck, ArpGain = 0.22f, ArpPattern = new[] { 0, -1, 2, -1, 3, -1 }, ArpOctave = 0,
        Bass = Inst.Pluck, BassGain = 0.3f, BassPattern = new[] { 0, -1, -1, -1, -1, -1 },
        PadOn = true, PadGain = 0.22f,
        Reverb = 0.4f, Brightness = 0.9f,
    };

    // "Light chiptune-folk": soft pulse waves, a bouncy bass, hats.
    public static readonly Arrangement Chip = new()
    {
        Name = "C-chiptune-folk",
        TempoScale = 1.2f,
        Melody = Inst.Square, MelodyGain = 0.55f,
        Arp = Inst.Pulse, ArpGain = 0.22f, ArpPattern = new[] { 0, 2, 3, 2, 3, 2 }, ArpOctave = 0,
        Bass = Inst.Triangle, BassGain = 0.55f, BassPattern = new[] { 0, -1, 3, 2, -1, 3 },
        Tick = Inst.Hat, TickGain = 0.11f,
        Reverb = 0.12f,
    };

    // The lullaby voicing for the nap song: soft flute over a music-box rock and a warm pad.
    public static readonly Arrangement Lullaby = new()
    {
        Name = "nap-lullaby",
        Melody = Inst.Flute, MelodyGain = 0.75f,
        Double = Inst.MusicBox, DoubleGain = 0.3f,
        Arp = Inst.MusicBox, ArpGain = 0.2f, ArpPattern = new[] { 0, -1, -1, 2, -1, -1 }, ArpOctave = 1,
        Bass = Inst.Triangle, BassGain = 0.4f, BassPattern = new[] { 0, -1, -1, 2, -1, -1 },
        PadOn = true, PadGain = 0.24f,
        Reverb = 0.42f, Brightness = 0.85f,
    };
}
