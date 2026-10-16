using System;
using System.Linq;
using Godot;

namespace LifeGame;

// The one list of world keys, matching KEYMAP.md. Main calls Setup before any lane
// binds its own keys; lanes' Bind helpers skip actions that already exist, so the
// keys here win without editing lane files.
public static class Controls
{
    // Help names the action in the F1 key list (ui/KeyHelp.cs); null leaves it out.
    // KeyText overrides how its keys are written there ("W A S D").
    // Hint is shown once, the first time a new player could use the key (ui/Hints.cs);
    // OnRequest hints wait for a lane to call Hints.Request(action) at the right moment.
    // A row with no Keys or Mouse belongs to a lane that still binds it itself; its
    // key list entry and hint wait until that action exists.
    public record Binding(string Action, Key[] Keys, string? Help, string? Hint = null,
        bool OnRequest = false, MouseButton Mouse = MouseButton.None, string? KeyText = null);

    static readonly Key[] None = Array.Empty<Key>();

    public static readonly Binding[] World =
    {
        new("walk_forward", new[] { Key.W, Key.Up }, "Walk", "W A S D to walk", KeyText: "W A S D"),
        new("walk_back", new[] { Key.S, Key.Down }, null),
        new("walk_left", new[] { Key.A, Key.Left }, null),
        new("walk_right", new[] { Key.D, Key.Right }, null),
        // Hints show in this order: walking, then picking up and making things (the
        // first things a new player needs), then the rest.
        new("pick_up", new[] { Key.Space }, "Pick up", "Space picks up what lies at your feet"),
        new("craft", new[] { Key.C }, "Make things", "C to make things from what you carry"),
        new("turn_left", new[] { Key.Q }, "Turn the view", "Q and E turn the view", KeyText: "Q  E"),
        new("turn_right", new[] { Key.E }, null),
        new("camera_drag", Array.Empty<Key>(), "Turn the view freely (hold and drag)", Mouse: MouseButton.Right, KeyText: "Right drag"),
        new("key_help", new[] { Key.F1 }, null, "Hold F1 to see every key"),
        new("sneak", new[] { Key.Shift }, "Sneak (hold)", "Hold Shift to sneak up on animals", OnRequest: true),
        new("use", new[] { Key.G }, "Use what's in front of you"),
        new("dig", new[] { Key.F }, "Dig", "F digs the ground in front of you"),
        // Animals binds strike (click, or Z) and Skills binds craft_play_minigame (Ctrl)
        // with these keys; they join the key list once those lanes land.
        new("strike", None, "Strike", "Click to strike", OnRequest: true),
        new("craft_play_minigame", None, "Work a craft by hand (hold while starting)"),
        new("eat", new[] { Key.X }, "Eat", "X eats the best food you carry"),
        new("drink", None, "Drink"),
        new("build_mode", new[] { Key.B }, "Build", "B to lay out a shelter"),
        new("journal", None, "Journal", "J opens your journal"),
        new("claim_open", None, "Your claim", "K shows the land you claim"),
        new("chat", None, "Speak", "Enter to speak"),
        new("toggle_music", new[] { Key.M }, "Music on or off"),
        // Interim: Ceramics reads V directly until tending clay moves to G.
        new("tend_clay", new[] { Key.V }, "Tend clay"),
    };

    public static void Setup()
    {
        foreach (var b in World)
        {
            if ((b.Keys.Length == 0 && b.Mouse == MouseButton.None) || InputMap.HasAction(b.Action)) continue;
            InputMap.AddAction(b.Action);
            foreach (var key in b.Keys) InputMap.ActionAddEvent(b.Action, new InputEventKey { PhysicalKeycode = key });
            if (b.Mouse != MouseButton.None) InputMap.ActionAddEvent(b.Action, new InputEventMouseButton { ButtonIndex = b.Mouse });
        }
    }

    // How a row's keys read right now, from the live input map, so a lane's own
    // binding shows correctly too. Null when the action isn't bound (yet).
    public static string? KeysFor(Binding b)
    {
        if (b.KeyText != null) return b.KeyText;
        if (!InputMap.HasAction(b.Action)) return null;
        var names = InputMap.ActionGetEvents(b.Action).Select(e => e switch
        {
            InputEventKey k => KeyName(k.PhysicalKeycode != Key.None ? k.PhysicalKeycode : k.Keycode),
            InputEventMouseButton { ButtonIndex: MouseButton.Left } => "Click",
            InputEventMouseButton { ButtonIndex: MouseButton.Right } => "Right click",
            _ => null,
        }).Where(n => n != null).Distinct().ToList();
        return names.Count > 0 ? string.Join("  ", names) : null;
    }

    static string KeyName(Key key) => key switch
    {
        Key.Ctrl => "Ctrl",
        Key.Shift => "Shift",
        Key.Space => "Space",
        Key.Enter or Key.KpEnter => "Enter",
        Key.Escape => "Esc",
        _ => OS.GetKeycodeString(key),
    };
}
