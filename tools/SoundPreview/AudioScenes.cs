using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LifeGame;

// Audio-lane preview scenes, kept out of Program.cs so new scenes never conflict with other lanes'
// edits there. A scene is a static method in its own file (Scenes/<Name>.cs) marked
// [Scene("name")]; found by reflection like [SubscriptionQueries], so adding one touches no shared
// file. Program.cs calls Owns (to leave the walking off) and Render once per block.
[AttributeUsage(AttributeTargets.Method)]
sealed class SceneAttribute : Attribute
{
    public readonly string Name;
    public SceneAttribute(string name) => Name = name;
}

// One block of audio being rendered: play cues in it with Mix, At(t) is true in the block where
// t seconds falls, A and B are its start and end seconds.
readonly record struct Block(SoundMix Mix, double A, double B)
{
    public bool At(double t) => A < t && B >= t;
}

static partial class AudioScenes
{
    static readonly Lazy<Dictionary<string, Action<Block>>> scenes = new(() =>
        typeof(AudioScenes).GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Select(m => (m, attr: m.GetCustomAttribute<SceneAttribute>()))
            .Where(x => x.attr != null)
            .ToDictionary(x => x.attr!.Name, x => (Action<Block>)Delegate.CreateDelegate(typeof(Action<Block>), x.m)));

    public static bool Owns(string scene) => scenes.Value.ContainsKey(scene);

    public static void Render(string scene, SoundMix mix, int done, int n, int rate)
    {
        if (scenes.Value.TryGetValue(scene, out var render))
            render(new Block(mix, done / (double)rate, (done + n) / (double)rate));
    }
}
