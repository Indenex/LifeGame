using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// The one list of world keys, matching KEYMAP.md. Main calls Setup before any lane
// binds its own keys; lanes' Bind helpers skip actions that already exist, so the
// keys here win without editing lane files.
public static class Controls
{
    // Help names the action in the F1 key list (ui/KeyHelp.cs); null leaves it out.
    // Hint is shown once, the first time a new player could use the key (ui/Hints.cs);
    // "{key}" in it reads the player's current key. OnRequest hints wait for a lane to
    // call Hints.Request(action) at the right moment.
    // Name labels the row on the Controls page (ui/ControlsPage.cs) when Help doesn't
    // fit; Joins lists actions whose first keys read together ("W A S D").
    // Fixed rows can't be rebound (a lane reads the raw key). KeyText writes a row's
    // keys for the F1 list when they aren't one action ("Shift {use}" reads the Use key).
    // A row with no Keys or Mouse belongs to a lane that still binds it itself; its
    // key list entry and hint wait until that action exists.
    public record Binding(string Action, Key[] Keys, string? Help, string? Hint = null,
        bool OnRequest = false, MouseButton Mouse = MouseButton.None, string? Name = null,
        string[]? Joins = null, bool Fixed = false, string? KeyText = null)
    {
        public string Label => Name ?? Help ?? Action;
    }

    static readonly Key[] None = Array.Empty<Key>();

    public static readonly Binding[] World =
    {
        new("walk_forward", new[] { Key.W, Key.Up }, "Walk", "{key} to walk", Name: "Walk forward", Joins: new[] { "walk_forward", "walk_left", "walk_back", "walk_right" }),
        new("walk_back", new[] { Key.S, Key.Down }, null, Name: "Walk back"),
        new("walk_left", new[] { Key.A, Key.Left }, null, Name: "Walk left"),
        new("walk_right", new[] { Key.D, Key.Right }, null, Name: "Walk right"),
        // Hints show in this order: walking, then picking up and making things (the
        // first things a new player needs), then the rest.
        new("pick_up", new[] { Key.Space }, "Pick up", "{key} picks up what lies at your feet"),
        new("craft", new[] { Key.C }, "Make things", "{key} to make things from what you carry"),
        new("turn_left", new[] { Key.Q }, "Turn the view", "{key} turn the view", Name: "Turn the view left", Joins: new[] { "turn_left", "turn_right" }),
        new("turn_right", new[] { Key.E }, null, Name: "Turn the view right"),
        new("camera_drag", Array.Empty<Key>(), "Turn the view freely (hold and drag)", Mouse: MouseButton.Right, Name: "Turn the view freely (hold and drag)"),
        new("key_help", new[] { Key.F1 }, null, "Hold {key} to see every key", Name: "Show every key (hold)"),
        // D-088, D-091: Shift runs (stamina, survival-7) and sneak is Ctrl.
        new("run", new[] { Key.Shift }, "Run (hold)", "Hold {key} to run", OnRequest: true, Name: "Run (hold)"),
        new("sneak", new[] { Key.Ctrl }, "Sneak (hold)", "Hold {key} to sneak up on animals", OnRequest: true),
        new("use", new[] { Key.G }, "Use what's in front of you"),
        // Building reads Shift inside its door offer, so this follows the Use key.
        new("bar_door", None, "Bar or unbar a hut door", Fixed: true, KeyText: "Shift {use}"),
        new("dig", new[] { Key.F }, "Dig", "{key} digs the ground in front of you"),
        // Animals binds strike (click, or Z) and Skills binds craft_play_minigame (Ctrl)
        // with these keys; they join the key list once those lanes land.
        new("strike", None, "Strike", "{key} to strike", OnRequest: true),
        new("craft_play_minigame", None, "Work a craft by hand (hold while starting)"),
        new("eat", new[] { Key.X }, "Eat", "{key} eats the best food you carry"),
        new("drink", None, "Drink"),
        new("build_mode", new[] { Key.B }, "Build", "{key} to lay out a shelter"),
        new("journal", None, "Journal", "{key} opens your journal"),
        new("claim_open", None, "Your claim", "{key} shows the land you claim"),
        new("chat", None, "Speak", "{key} to speak"),
        new("toggle_music", new[] { Key.M }, "Music on or off"),
        // Interim: Ceramics reads V directly until tending clay moves to G.
        new("tend_clay", new[] { Key.V }, "Tend clay", Fixed: true),
    };

    // What each action was bound to before the player changed anything, so Reset
    // can put it back.
    static readonly Dictionary<string, InputEvent[]> defaults = new();

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

    // Rows the player can rebind: bound now, and not read as a raw key by a lane.
    public static IEnumerable<Binding> Rebindable =>
        World.Where(b => !b.Fixed && (b.Help != null || b.Name != null) && InputMap.HasAction(b.Action));

    // Runs once every lane has bound its keys (deferred from Main's _Ready): remembers
    // the defaults, then applies the player's saved keys.
    public static void ApplySaved()
    {
        foreach (var b in World)
            if (InputMap.HasAction(b.Action) && !defaults.ContainsKey(b.Action))
                defaults[b.Action] = InputMap.ActionGetEvents(b.Action).ToArray();
        foreach (var b in Rebindable.ToList())
        {
            string saved = Settings.Get("keys", b.Action, "");
            if (saved == "none") InputMap.ActionEraseEvents(b.Action);
            else if (Decode(saved) is { } e) Set(b.Action, e);
        }
    }

    // Binds an action to one key or button and saves it. If another action had that
    // key, the two swap, so nothing is left doing two things at once.
    public static void Rebind(string action, InputEvent e)
    {
        var old = InputMap.ActionGetEvents(action).FirstOrDefault();
        foreach (var other in Rebindable)
        {
            if (other.Action == action || !InputMap.ActionGetEvents(other.Action).Any(x => Same(x, e))) continue;
            if (old != null) { Set(other.Action, old); Save(other.Action, old); }
            else { InputMap.ActionEraseEvents(other.Action); Settings.Set("keys", other.Action, "none"); }
        }
        Set(action, e);
        Save(action, e);
    }

    public static void ResetAll()
    {
        foreach (var (action, events) in defaults)
        {
            if (!InputMap.HasAction(action)) continue;
            InputMap.ActionEraseEvents(action);
            foreach (var e in events) InputMap.ActionAddEvent(action, e);
        }
        Settings.Clear("keys");
    }

    static void Set(string action, InputEvent e)
    {
        InputMap.ActionEraseEvents(action);
        InputMap.ActionAddEvent(action, e);
    }

    static void Save(string action, InputEvent e) => Settings.Set("keys", action, Encode(e) ?? "none");

    static bool Same(InputEvent a, InputEvent b) => (a, b) switch
    {
        (InputEventKey x, InputEventKey y) => KeyOf(x) == KeyOf(y),
        (InputEventMouseButton x, InputEventMouseButton y) => x.ButtonIndex == y.ButtonIndex,
        _ => false,
    };

    static Key KeyOf(InputEventKey k) => k.PhysicalKeycode != Key.None ? k.PhysicalKeycode : k.Keycode;

    static string? Encode(InputEvent e) => e switch
    {
        InputEventKey k => $"key:{(long)KeyOf(k)}",
        InputEventMouseButton m => $"mouse:{(long)m.ButtonIndex}",
        _ => null,
    };

    static InputEvent? Decode(string text)
    {
        var parts = text.Split(':');
        if (parts.Length != 2 || !long.TryParse(parts[1], out var code)) return null;
        return parts[0] switch
        {
            "key" => new InputEventKey { PhysicalKeycode = (Key)code },
            "mouse" => new InputEventMouseButton { ButtonIndex = (MouseButton)code },
            _ => null,
        };
    }

    // How a row's keys read right now, from the live input map, so a lane's own
    // binding and the player's own keys both show correctly. Null when unbound.
    public static string? KeysFor(Binding b)
    {
        if (b.KeyText != null) return b.KeyText.Replace("{use}", KeyFor("use") ?? "G");
        if (b.Joins != null)
        {
            var firsts = b.Joins.Select(a => Names(a).FirstOrDefault()).ToList();
            return firsts.Any(n => n == null) ? null : string.Join(firsts.All(n => n!.Length == 1) ? " " : "  ", firsts);
        }
        var names = Names(b.Action);
        if (b.Action == "camera_drag") names = names.Select(n => n.Replace(" click", "") + " drag").ToList();
        return names.Count > 0 ? string.Join("  ", names) : null;
    }

    // The first key of an action as a hint or prompt writes it ("G"), or null.
    public static string? KeyFor(string action)
    {
        var row = World.FirstOrDefault(b => b.Action == action);
        return row?.Joins != null ? KeysFor(row) : Names(action).FirstOrDefault();
    }

    // A hint with "{key}" filled in from the live binding.
    public static string HintText(Binding b) =>
        b.Hint!.Replace("{key}", (b.Joins != null ? KeysFor(b) : KeyFor(b.Action)) ?? "?");

    static List<string> Names(string action) =>
        !InputMap.HasAction(action) ? new() :
        InputMap.ActionGetEvents(action).Select(EventName).OfType<string>().Distinct().ToList();

    public static string? EventName(InputEvent e) => e switch
    {
        InputEventKey k => KeyName(KeyOf(k)),
        InputEventMouseButton { ButtonIndex: MouseButton.Left } => "Click",
        InputEventMouseButton { ButtonIndex: MouseButton.Right } => "Right click",
        InputEventMouseButton { ButtonIndex: MouseButton.Middle } => "Middle click",
        InputEventMouseButton m => $"Mouse {(int)m.ButtonIndex}",
        _ => null,
    };

    static string KeyName(Key key) => key switch
    {
        Key.Ctrl => "Ctrl",
        Key.Shift => "Shift",
        Key.Alt => "Alt",
        Key.Space => "Space",
        Key.Tab => "Tab",
        Key.Enter or Key.KpEnter => "Enter",
        Key.Escape => "Esc",
        Key.Up => "Up",
        Key.Down => "Down",
        Key.Left => "Left",
        Key.Right => "Right",
        _ => OS.GetKeycodeString(key),
    };
}
