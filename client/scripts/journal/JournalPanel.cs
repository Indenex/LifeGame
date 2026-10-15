using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The journal page: skills on the left, everything you have discovered on the
// right. Fades in and out softly; drawn in the same quiet style as the HUD.
public partial class JournalPanel : Control
{
    public Action<string> WriteNote = _ => { };
    public Action<string, SkillMode> SetMode = (_, _) => { };
    public bool IsOpen { get; private set; }

    readonly VBoxContainer skillList = new();
    readonly VBoxContainer entryList = new();
    readonly Label capLabel = new();
    readonly Label undiscovered = new();
    Tween? fade;

    static readonly Color Ink = new(Look.Cream, 0.95f);
    static readonly Color Faint = new(Look.Cream, 0.55f);
    static readonly Color Accent = new(0.86f, 0.72f, 0.45f);

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        Modulate = new Color(1, 1, 1, 0);

        var page = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        page.AddThemeStyleboxOverride("panel", Box(new Color(0.09f, 0.10f, 0.08f, 0.82f), 18, 26));
        page.CustomMinimumSize = new Vector2(860, 520);
        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        center.AddChild(page);
        AddChild(center);

        var rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 14);
        page.AddChild(rows);

        var header = new HBoxContainer();
        header.AddChild(Text("Journal", 26, Ink));
        header.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        header.AddChild(Text("J to close", 14, Faint));
        rows.AddChild(header);

        var columns = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 28);
        rows.AddChild(columns);

        var left = new VBoxContainer { CustomMinimumSize = new Vector2(330, 0) };
        left.AddThemeConstantOverride("separation", 10);
        left.AddChild(Text("Skills", 18, Accent));
        Style(capLabel, 14, Faint);
        left.AddChild(capLabel);
        skillList.AddThemeConstantOverride("separation", 10);
        left.AddChild(skillList);
        columns.AddChild(left);

        var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 10);
        right.AddChild(Text("Discoveries", 18, Accent));
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        entryList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        entryList.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(entryList);
        right.AddChild(scroll);
        Style(undiscovered, 14, Faint);
        right.AddChild(undiscovered);
        columns.AddChild(right);
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        fade?.Kill();
        fade = CreateTween().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        if (IsOpen)
        {
            Visible = true;
            fade.TweenProperty(this, "modulate:a", 1f, 0.25f);
        }
        else
        {
            fade.TweenProperty(this, "modulate:a", 0f, 0.2f);
            fade.TweenCallback(Callable.From(() => Visible = false));
        }
    }

    public void SetData(List<JournalEntry> entries, List<SkillLevel> levels, List<Taught> taught)
    {
        if (!IsInsideTree()) return;
        var bySpec = levels.ToDictionary(s => s.Spec);

        Clear(skillList);
        int total = levels.Where(s => Skills.Spec(s.Spec) is { Basic: false }).Sum(s => Skills.Rank(s.Points));
        capLabel.Text = $"{total} of {Skills.TotalCap} specialist ranks used. Survival basics are free.";
        foreach (var spec in Skills.Specializations.OrderBy(s => s.Basic ? 0 : 1))
        {
            bySpec.TryGetValue(spec.Id, out var level);
            skillList.AddChild(SkillRow(spec, level));
        }

        Clear(entryList);
        if (entries.Count == 0)
            entryList.AddChild(Wrapped("Nothing yet. Try things: dig, gather, work with what you find. Watching someone else work, or reading a note they left, teaches you too.", 15, Faint));
        foreach (var e in entries.OrderBy(e => e.LearnedAt.MicrosecondsSinceUnixEpoch))
            entryList.AddChild(EntryRow(e, bySpec));

        var known = entries.Select(e => e.ProcessId).ToHashSet();
        int left = Skills.AllProcessIds().Count(id => !known.Contains(id));
        undiscovered.Text = left > 0 ? $"{left} more {(left == 1 ? "thing" : "things")} to discover." : "You have found everything there is, for now.";
        int pupils = taught.Select(t => t.LearnerName).Distinct().Count();
        if (pupils > 0)
            undiscovered.Text += $"  You have taught {pupils} {(pupils == 1 ? "person" : "people")} {taught.Count} {(taught.Count == 1 ? "thing" : "things")}.";
    }

    Control SkillRow(Specialization spec, SkillLevel? level)
    {
        int points = level?.Points ?? 0;
        int rank = Skills.Rank(points);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);

        var top = new HBoxContainer();
        top.AddChild(Text(spec.Name, 16, Ink));
        top.AddChild(Text($"  {Skills.Title(rank)}", 14, Faint));
        top.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        top.AddChild(Text(rank.ToString(), 16, Ink));
        if (!spec.Basic) top.AddChild(ModeButton(spec.Id, level?.Mode ?? SkillMode.Raise));
        box.AddChild(top);

        var bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = Skills.MaxPoints,
            Value = points,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 6),
            TooltipText = spec.Blurb,
        };
        bar.AddThemeStyleboxOverride("background", Box(new Color(1, 1, 1, 0.08f), 3, 0));
        bar.AddThemeStyleboxOverride("fill", Box(spec.Basic ? new Color(0.58f, 0.68f, 0.40f) : Accent, 3, 0));
        box.AddChild(bar);
        return box;
    }

    // Raise, hold or fade. Only matters once you reach the total cap.
    Button ModeButton(string specId, SkillMode mode)
    {
        var (glyph, tip) = mode switch
        {
            SkillMode.Hold => ("=", "Hold: stays where it is"),
            SkillMode.Fade => ("v", "Fade: gives up ranks so other skills can grow past the cap"),
            _ => ("^", "Raise: grows as you use it"),
        };
        var next = mode switch { SkillMode.Raise => SkillMode.Hold, SkillMode.Hold => SkillMode.Fade, _ => SkillMode.Raise };
        var button = new Button { Text = glyph, TooltipText = $"{tip}. Click to change.", Flat = true, CustomMinimumSize = new Vector2(30, 0) };
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeColorOverride("font_color", mode == SkillMode.Raise ? Accent : Faint);
        button.Pressed += () => SetMode(specId, next);
        return button;
    }

    Control EntryRow(JournalEntry e, Dictionary<string, SkillLevel> bySpec)
    {
        var spec = Skills.SpecFor(e.ProcessId);
        int rank = spec != null && bySpec.TryGetValue(spec.Id, out var level) ? Skills.Rank(level.Points) : 0;

        var card = new PanelContainer();
        card.AddThemeStyleboxOverride("panel", Box(new Color(1, 1, 1, 0.04f), 10, 12));
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 3);
        card.AddChild(box);

        var top = new HBoxContainer();
        top.AddChild(Text(Skills.ProcessName(e.ProcessId), 16, Ink));
        top.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var note = new Button { Text = "Leave a note", Flat = true, TooltipText = "Drop a note at your feet so others can learn this" };
        note.AddThemeFontSizeOverride("font_size", 13);
        note.AddThemeColorOverride("font_color", Accent);
        note.Pressed += () => WriteNote(e.ProcessId);
        top.AddChild(note);
        box.AddChild(top);

        string how = e.LearnedBy switch
        {
            LearnedBy.Watching => $"Learned by watching {e.LearnedFrom}",
            LearnedBy.Reading => $"Learned from {e.LearnedFrom}'s note",
            _ => "Worked it out yourself",
        };
        string times = e.TimesPerformed switch { 0 => "not tried yet", 1 => "done once", var n => $"done {n} times" };
        box.AddChild(Text($"{how}, {times}", 13, Faint));

        if (spec != null)
        {
            int speed = (int)MathF.Round(100f / Skills.DurationScale(rank));
            box.AddChild(Text($"{spec.Name}    Speed {speed}%    Best quality {Processes.Grade(Skills.QualityCap(rank))}", 13, Faint));
        }
        return card;
    }

    static void Clear(Node node)
    {
        foreach (var child in node.GetChildren()) child.QueueFree();
    }

    static Label Text(string text, int size, Color color)
    {
        var label = new Label { Text = text };
        Style(label, size, color);
        return label;
    }

    static Label Wrapped(string text, int size, Color color)
    {
        var label = Text(text, size, color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(200, 0);
        return label;
    }

    static void Style(Label label, int size, Color color)
    {
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.35f));
        label.AddThemeConstantOverride("shadow_offset_y", 1);
    }

    static StyleBoxFlat Box(Color color, int radius, int margin)
    {
        var style = new StyleBoxFlat
        {
            BgColor = color,
            ContentMarginLeft = margin,
            ContentMarginRight = margin,
            ContentMarginTop = margin * 0.7f,
            ContentMarginBottom = margin * 0.7f,
        };
        style.SetCornerRadiusAll(radius);
        return style;
    }
}
