using System;
using Godot;

namespace LifeGame;

// Plays the recorded music (audio-8): OGG files from client/assets/music/, listed in
// MusicDefs. It lives outside the synth thread, on its own "Music" bus: two stream players
// cross-fade when the mood changes, and long silences separate tracks. MusicDirector decides
// what plays; this node loads, plays and fades. The Music slider and on/off come from
// SoundScape (the same settings as before), and the synthesized music is off.
public partial class MusicPlayer : Node
{
    const string BusName = "Music";

    readonly SoundScape scape;
    readonly MusicDirector director = new(MusicDefs.All, new Random());
    readonly AudioStreamPlayer[] players = { new(), new() };
    readonly float[] gain = new float[2];     // each player's fade level, 0 to 1
    readonly float[] fadeTo = new float[2];
    readonly float[] fadeRate = new float[2]; // per second
    readonly float[] trim = new float[2];     // linear, from the track's TrimDb
    int current = -1;
    int bus;
    readonly System.Collections.Generic.HashSet<string> missing = new();

    public MusicPlayer(SoundScape scape) => this.scape = scape;

    public override void _Ready()
    {
        bus = AudioServer.GetBusIndex(BusName);
        if (bus < 0)
        {
            AudioServer.AddBus();
            bus = AudioServer.BusCount - 1;
            AudioServer.SetBusName(bus, BusName);
            AudioServer.SetBusSend(bus, "Master");
        }
        for (int i = 0; i < 2; i++)
        {
            players[i].Bus = BusName;
            AddChild(players[i]);
            int slot = i;
            players[i].Finished += () => OnFinished(slot);
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // The slider sets the Music bus; 0.5 is the designed level, capped at +6 dB.
        float level = Math.Min(SoundMix.SliderGain(scape.MusicVolume), 2f);
        AudioServer.SetBusVolumeDb(bus, level <= 0.0001f ? -80f : Mathf.LinearToDb(level));

        var command = director.Update(dt, new MusicContext(scape.TimeOfDayNow, scape.RainNow, scape.CoverNow, scape.FireNow, scape.MusicOn));
        switch (command.Action)
        {
            case MusicAction.Start when command.Track is { } track:
                StartTrack(track, command.FadeSeconds);
                break;
            case MusicAction.Stop:
                FadeOutCurrent(command.FadeSeconds);
                current = -1;
                break;
        }

        // Fade every player toward its target; stop the ones that have faded out.
        for (int i = 0; i < 2; i++)
        {
            if (gain[i] != fadeTo[i])
            {
                float step = fadeRate[i] * dt;
                gain[i] = gain[i] < fadeTo[i] ? Math.Min(fadeTo[i], gain[i] + step) : Math.Max(fadeTo[i], gain[i] - step);
            }
            if (players[i].Playing)
            {
                players[i].VolumeDb = gain[i] <= 0.0001f ? -80f : Mathf.LinearToDb(gain[i] * trim[i]);
                if (gain[i] <= 0.0001f && fadeTo[i] <= 0f) players[i].Stop();
            }
        }
    }

    void StartTrack(MusicDef track, float fade)
    {
        if (!ResourceLoader.Exists(track.File))
        {
            if (missing.Add(track.Id)) GD.PushError($"Music: {track.File} is listed in MusicDefs but is not in the project; skipped.");
            director.TrackEnded();
            return;
        }
        var stream = GD.Load<AudioStream>(track.File);
        if (stream == null)
        {
            if (missing.Add(track.Id)) GD.PushError($"Music: {track.File} could not be loaded as audio; skipped.");
            director.TrackEnded();
            return;
        }
        FadeOutCurrent(fade);
        int slot = current == 0 ? 1 : 0;
        if (players[slot].Playing) players[slot].Stop();
        players[slot].Stream = stream;
        trim[slot] = Mathf.DbToLinear(track.TrimDb);
        gain[slot] = 0f;
        fadeTo[slot] = 1f;
        fadeRate[slot] = 1f / Math.Max(0.1f, fade);
        players[slot].VolumeDb = -80f;
        players[slot].Play();
        current = slot;
    }

    void FadeOutCurrent(float fade)
    {
        if (current < 0) return;
        fadeTo[current] = 0f;
        fadeRate[current] = Math.Max(0.05f, gain[current]) / Math.Max(0.1f, fade);
    }

    void OnFinished(int slot)
    {
        // Only the track that is meant to be playing ends the sequence; a faded-out one just stops.
        if (slot == current)
        {
            current = -1;
            director.TrackEnded();
        }
    }
}
