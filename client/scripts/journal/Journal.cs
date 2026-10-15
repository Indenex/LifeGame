using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LifeGame;

// The discovery journal and skills: keeps the player's journal and skill rows in
// sync, shows notes other players left on the ground, and opens the journal
// panel. J opens the journal, N reads the nearest note.
public partial class Journal : CanvasLayer
{
    // Added to the 3D world by Main, so notes on the ground render with everything else.
    public readonly Node3D Notes = new() { Name = "JournalNotes" };

    readonly JournalPanel panel = new();
    readonly Label prompt = new();
    readonly Dictionary<ulong, Node3D> noteNodes = new();
    readonly Dictionary<string, int> ranks = new();
    DbConnection? conn;
    SubscriptionHandle? subscription;
    (int, int) subscribedChunk = (int.MinValue, int.MinValue);
    Func<Vector3> playerPos = () => Vector3.Zero;
    Func<float, float, float> heightAt = (_, _) => 0f;
    Action<string> say = _ => { };
    bool caughtUp;
    float bob;
    static Mesh? noteMesh;
    static Journal? current;

    // The player's rank, 0 to 100, in the skill a process trains. Crafting asks this
    // to shorten minigames (see CraftingUi.Skills.cs).
    public static int RankFor(string processId) =>
        current?.conn is { } c && Skills.SpecFor(processId) is { } spec
            ? c.Db.MySkills.Iter().Where(s => s.Spec == spec.Id).Select(s => Skills.Rank(s.Points)).FirstOrDefault()
            : 0;

    public override void _Ready()
    {
        current = this;
        Layer = 2;
        AddChild(panel);

        // Shown only while a note is within reach.
        prompt.AddThemeFontSizeOverride("font_size", 16);
        prompt.AddThemeColorOverride("font_color", new Color(Look.Cream, 0.9f));
        prompt.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.45f));
        prompt.AddThemeConstantOverride("shadow_offset_y", 1);
        prompt.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        prompt.GrowHorizontal = Control.GrowDirection.Both;
        prompt.HorizontalAlignment = HorizontalAlignment.Center;
        prompt.Position += new Vector2(0, -120);
        prompt.Visible = false;
        AddChild(prompt);
        panel.WriteNote = id =>
        {
            if (conn == null) return;
            conn.Reducers.WriteNote(id);
            say($"You leave a note about {Skills.ProcessName(id).ToLowerInvariant()} at your feet");
        };
        panel.SetMode = (spec, mode) => conn?.Reducers.SetSkillMode(spec, mode);
        Bind("journal", Key.J);
        Bind("read_note", Key.N);
        Refresh();
        // --journal opens the journal on start, for screenshots.
        if (OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--journal"))
            GetTree().CreateTimer(1.0).Timeout += panel.Toggle;
    }

    public void Attach(DbConnection connection, Func<Vector3> player, Func<float, float, float> height, Action<string> note)
    {
        conn = connection;
        playerPos = player;
        heightAt = height;
        say = note;

        conn.Db.MyJournal.OnInsert += (_, e) => { Learned(e); Refresh(); };
        conn.Db.MyJournal.OnUpdate += (_, _, _) => Refresh();
        conn.Db.MySkills.OnInsert += (_, s) => { SkillChanged(s); Refresh(); };
        conn.Db.MySkills.OnUpdate += (_, _, s) => { SkillChanged(s); Refresh(); };
        conn.Db.JournalNote.OnInsert += (_, n) => AddNote(n);
        conn.Db.JournalNote.OnDelete += (_, n) => RemoveNote(n.Id);
    }

    public void Detach()
    {
        conn = null;
        Refresh();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("journal"))
        {
            panel.Toggle();
            GetViewport().SetInputAsHandled();
        }
        else if (e.IsActionPressed("ui_cancel") && panel.IsOpen)
        {
            panel.Toggle();
            GetViewport().SetInputAsHandled();
        }
        else if (e.IsActionPressed("read_note"))
        {
            ReadNearestNote();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (conn == null) return;
        var at = playerPos();
        Follow(World.ChunkOf(at.X), World.ChunkOf(at.Z));

        var near = NearestNote(at);
        prompt.Visible = near != null && !panel.IsOpen;
        if (near != null) prompt.Text = $"N  Read {near.AuthorName}'s note";

        // Notes stir a little, as if the breeze catches them.
        bob += (float)delta;
        foreach (var (id, node) in noteNodes)
            node.Rotation = new Vector3(Mathf.Sin(bob * 1.3f + id) * 0.05f, node.Rotation.Y, 0);
    }

    // Notes are only needed nearby; the journal and skills are always subscribed.
    void Follow(int chunkX, int chunkZ)
    {
        if (conn is not { IsActive: true } || subscribedChunk == (chunkX, chunkZ)) return;
        subscribedChunk = (chunkX, chunkZ);
        int r = World.ViewRadiusChunks;
        var old = subscription;
        subscription = conn.SubscriptionBuilder()
            .OnApplied(_ =>
            {
                old?.Unsubscribe();
                caughtUp = true;
            })
            .OnError((_, e) => say($"Journal subscription error: {e.Message}"))
            .Subscribe(new[]
            {
                "SELECT * FROM my_journal",
                "SELECT * FROM my_skills",
                $"SELECT * FROM journal_note WHERE chunk_x >= {chunkX - r} AND chunk_x <= {chunkX + r} AND chunk_z >= {chunkZ - r} AND chunk_z <= {chunkZ + r}",
            });
    }

    void Refresh()
    {
        if (!IsInsideTree()) return;
        var entries = conn?.Db.MyJournal.Iter().ToList() ?? new List<JournalEntry>();
        var skills = conn?.Db.MySkills.Iter().ToList() ?? new List<SkillLevel>();
        panel.SetData(entries, skills);
    }

    // Quiet notes when you learn something new, but not for what you already knew at login.
    void Learned(JournalEntry e)
    {
        if (!caughtUp) return;
        string name = Skills.ProcessName(e.ProcessId);
        say(e.LearnedBy switch
        {
            LearnedBy.Watching => $"You picked up {name.ToLowerInvariant()} by watching {e.LearnedFrom}",
            LearnedBy.Reading => $"You learned {name.ToLowerInvariant()} from {e.LearnedFrom}'s note",
            _ => $"New in your journal: {name}",
        });
    }

    void SkillChanged(SkillLevel s)
    {
        int rank = Skills.Rank(s.Points);
        bool known = ranks.TryGetValue(s.Spec, out int before);
        ranks[s.Spec] = rank;
        if (!caughtUp || !known || rank <= before) return;
        string name = Skills.Spec(s.Spec)?.Name ?? s.Spec;
        string title = Skills.Title(rank);
        say(title != Skills.Title(before) ? $"{name} {rank}: you are now {Article(title)} {title.ToLowerInvariant()}" : $"{name} {rank}");
    }

    static string Article(string word) => "AEIOU".Contains(word[0]) ? "an" : "a";

    void ReadNearestNote()
    {
        if (conn == null) return;
        if (NearestNote(playerPos()) is not { } note)
        {
            say("No note within reach");
            return;
        }
        conn.Reducers.ReadNote(note.Id);
    }

    JournalNote? NearestNote(Vector3 at) => conn?.Db.JournalNote.Iter()
        .Select(n => (note: n, d: new Vector2(n.X - at.X, n.Z - at.Z).Length()))
        .Where(t => t.d <= World.Reach)
        .OrderBy(t => t.d)
        .Select(t => t.note)
        .FirstOrDefault();

    void AddNote(JournalNote n)
    {
        if (noteNodes.ContainsKey(n.Id)) return;
        var node = new MeshInstance3D
        {
            Mesh = noteMesh ??= Look.Faceted(new BoxMesh { Size = new Vector3(0.26f, 0.025f, 0.18f) }, new Color(0.86f, 0.80f, 0.66f)),
            Position = new Vector3(n.X, heightAt(n.X, n.Z) + 0.03f, n.Z),
            Rotation = new Vector3(0, Terrain.Hash01((int)n.Id, 7, 33) * Mathf.Tau, 0),
        };
        noteNodes[n.Id] = node;
        Notes.AddChild(node);
    }

    void RemoveNote(ulong id)
    {
        if (noteNodes.Remove(id, out var node)) node.QueueFree();
    }

    static void Bind(string action, Key key)
    {
        if (InputMap.HasAction(action)) return;
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }
}
