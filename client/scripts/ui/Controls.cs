using System;
using Godot;

namespace LifeGame;

// The one list of world keys, matching KEYMAP.md. Main calls Setup before any lane
// binds its own keys; lanes' Bind helpers skip actions that already exist, so the
// keys here win without editing lane files.
public static class Controls
{
    // Hint is shown once, the first time a new player could use the key (ui/Hints.cs);
    // null means the key needs none. A row with no Keys only carries a hint for an
    // action a lane still binds itself; that hint waits until the lane's action exists.
    public record Binding(string Action, Key[] Keys, string? Hint);

    public static readonly Binding[] World =
    {
        new("walk_forward", new[] { Key.W, Key.Up }, "W A S D to walk"),
        new("walk_back", new[] { Key.S, Key.Down }, null),
        new("walk_left", new[] { Key.A, Key.Left }, null),
        new("walk_right", new[] { Key.D, Key.Right }, null),
        new("turn_left", new[] { Key.Q }, "Q and E turn the view"),
        new("turn_right", new[] { Key.E }, null),
        new("pick_up", new[] { Key.Space }, "Space picks up what lies at your feet"),
        new("use", new[] { Key.G }, null),
        new("craft", new[] { Key.C }, "C to make things from what you carry"),
        new("dig", new[] { Key.F }, "F digs the ground in front of you"),
        new("eat", new[] { Key.X }, "X eats the best food you carry"),
        new("build_mode", new[] { Key.B }, "B to lay out a shelter"),
        new("toggle_music", new[] { Key.M }, null),
        // Interim: Ceramics reads V directly until tending clay moves to G.
        new("tend_clay", new[] { Key.V }, null),
        new("sneak", Array.Empty<Key>(), "Hold Shift to move quietly"),
        new("journal", Array.Empty<Key>(), "J opens your journal"),
        new("claim_open", Array.Empty<Key>(), "K shows the land you claim"),
        new("chat", Array.Empty<Key>(), "Enter to speak"),
    };

    public static void Setup()
    {
        foreach (var b in World)
        {
            if (b.Keys.Length == 0 || InputMap.HasAction(b.Action)) continue;
            InputMap.AddAction(b.Action);
            foreach (var key in b.Keys) InputMap.ActionAddEvent(b.Action, new InputEventKey { PhysicalKeycode = key });
        }
    }
}
