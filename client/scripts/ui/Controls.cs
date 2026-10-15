using System.Linq;
using Godot;

namespace LifeGame;

// The one list of world keys, matching KEYMAP.md. Main calls Setup before any lane
// binds its own keys; lanes' Bind helpers skip actions that already exist, so the
// keys here win without editing lane files.
public static class Controls
{
    // Help is the text in the controls line; null keeps a key out of it.
    public record Binding(string Action, Key[] Keys, string? Help);

    public static readonly Binding[] World =
    {
        new("walk_forward", new[] { Key.W, Key.Up }, "WASD walk"),
        new("walk_back", new[] { Key.S, Key.Down }, null),
        new("walk_left", new[] { Key.A, Key.Left }, null),
        new("walk_right", new[] { Key.D, Key.Right }, null),
        new("turn_left", new[] { Key.Q }, "Q/E turn"),
        new("turn_right", new[] { Key.E }, null),
        new("pick_up", new[] { Key.Space }, "Space pick up"),
        new("dig", new[] { Key.F }, "F dig"),
        new("use", new[] { Key.G }, "G use"),
        new("craft", new[] { Key.C }, "C craft"),
        new("eat", new[] { Key.X }, "X eat"),
        new("build_mode", new[] { Key.B }, "B build"),
        // Interim: Ceramics reads V directly until tending clay moves to G.
        new("tend_clay", new[] { Key.V }, "V tend clay"),
        new("toggle_music", new[] { Key.M }, "M music"),
    };

    public static void Setup()
    {
        foreach (var b in World)
        {
            if (InputMap.HasAction(b.Action)) continue;
            InputMap.AddAction(b.Action);
            foreach (var key in b.Keys) InputMap.ActionAddEvent(b.Action, new InputEventKey { PhysicalKeycode = key });
        }
    }

    // The bottom-left help line, built from the table so it can't drift from the real keys.
    public static string HelpLine() =>
        string.Join("   ", World.Where(b => b.Help != null).Select(b => b.Help).Append("scroll zoom"));
}
