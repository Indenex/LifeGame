using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The crafting menu's pictures (ui-24; Justin: "better crafting menu with pictures"), a
// twin of the build menu: category tabs and a search box along the top, then a grid of
// cards. Each card shows what it makes, the inputs as small pictures with have/need
// (amber where you carry too little), the tool it wants and whether it is hands-on;
// a card you can't make yet is greyed and says why when you point at it. Clicking a
// card makes one. CraftingUi owns the rules (Missing, Begin) and hands them in, so this
// file only draws. Recipes come from shared Processes, so a new one shows up here
// with its picture and category without UI code (D-206).
public partial class CraftGrid : VBoxContainer
{
    public Func<IEnumerable<ProcessDef>> Recipes = () => Enumerable.Empty<ProcessDef>();
    public Func<ProcessDef, string?> Missing = _ => null;
    public Func<Item, uint> Have = _ => 0;
    public Action<ProcessDef> Make = _ => { };
    // The short note on a hands-on step ("hands-on", or "by heart" for a Master).
    public Func<ProcessDef, string> HandsOn = _ => "";

    const int Picture = 64;
    public const int Columns = 4;

    readonly HBoxContainer tabs = new();
    readonly LineEdit search = new() { PlaceholderText = "Search", ClearButtonEnabled = true, CustomMinimumSize = new Vector2(180, 0) };
    readonly GridContainer grid = new() { Columns = Columns };
    readonly ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
    string category = "";

    // Tabs in this order; a category not listed comes after, so a new one still shows.
    static readonly string[] Preferred = { "Tools", "Fire and food", "Cord and weaving", "Clay", "Hide and clothing", "Other" };

    // Which tab a recipe sits under, from what it makes and its process family.
    public static string CategoryOf(ProcessDef p)
    {
        string family = p.Id.Split('.')[0];
        var made = Items.IsKnown((ushort)p.Makes) && p.Outcome != Outcome.Fire ? Items.Def(p.Makes) : null;
        if (p.Outcome is Outcome.Fire or Outcome.Roast || family is "fire" or "cook"
            || made?.Use is UseAction.Eat or UseAction.Drink) return "Fire and food";
        if (family == "clay") return "Clay";
        if (family is "hide" or "sew" || made?.Use == UseAction.Wear || (made != null && Clothing.Find((ushort)made.Id) != null)) return "Hide and clothing";
        if (family is "cord" or "weave" or "gather" or "spin") return "Cord and weaving";
        if (made is { Wears: true } || made?.Use == UseAction.Tool || family is "knap" or "haft" or "lash" or "carve" or "chop") return "Tools";
        return "Other";
    }

    // The cards in the order they are shown; number keys 1 to 9 pick from these.
    public List<ProcessDef> Shown { get; private set; } = new();

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 10);
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 12);
        tabs.AddThemeConstantOverride("separation", 6);
        tabs.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        top.AddChild(tabs);
        search.AddThemeFontSizeOverride("font_size", 14);
        search.TextChanged += _ => Refresh();
        top.AddChild(search);
        AddChild(top);
        grid.AddThemeConstantOverride("h_separation", 10);
        grid.AddThemeConstantOverride("v_separation", 10);
        scroll.AddChild(grid);
        AddChild(scroll);
    }

    // The search box holds the keyboard while you type in it.
    public bool Typing => search.HasFocus();

    // Screenshots (--screenshot-craft): --screenshot-craft-search=axe types into the search
    // box, and --screenshot-craft-hover pins the first can't-make card's hover text beside it,
    // since a still can't point the mouse.
    public void Preview()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--screenshot-craft-search="))
            {
                search.Text = arg["--screenshot-craft-search=".Length..];
                Refresh();
            }
        if (!OS.GetCmdlineUserArgs().Contains("--screenshot-craft-hover")) return;
        GetTree().CreateTimer(0.5).Timeout += () =>
        {
            int i = Shown.FindIndex(p => Missing(p) != null);
            if (i < 0 || grid.GetChild(i) is not Control card) return;
            var tip = new PanelContainer { TopLevel = true, MouseFilter = MouseFilterEnum.Ignore };
            var style = new StyleBoxFlat { BgColor = new Color(0.08f, 0.08f, 0.07f, 0.95f), BorderColor = new Color(Look.Cream, 0.3f) };
            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(6);
            style.ContentMarginLeft = style.ContentMarginRight = 10;
            style.ContentMarginTop = style.ContentMarginBottom = 6;
            tip.AddThemeStyleboxOverride("panel", style);
            tip.AddChild(Hud.Text(card.TooltipText, 14));
            AddChild(tip);
            tip.GlobalPosition = card.GlobalPosition + new Vector2(card.Size.X * 0.6f, card.Size.Y * 0.7f);
        };
    }

    public void ClearSearch()
    {
        search.Text = "";
        search.ReleaseFocus();
    }

    List<string> Categories() => Preferred
        .Concat(Recipes().Select(CategoryOf).Distinct().Where(c => !Preferred.Contains(c)))
        .Where(c => Recipes().Any(p => CategoryOf(p) == c)).ToList();

    // Tab (and Shift+Tab) step through the categories.
    public void NextCategory(int step)
    {
        var all = Categories();
        if (all.Count == 0) return;
        int at = Math.Max(0, all.IndexOf(category));
        category = all[(at + step + all.Count) % all.Count];
        Refresh();
    }

    public void Refresh()
    {
        if (!IsInsideTree()) return;
        var all = Categories();
        if (!all.Contains(category)) category = all.FirstOrDefault(c => Recipes().Any(p => CategoryOf(p) == c && Missing(p) == null)) ?? all.FirstOrDefault() ?? "";
        string query = search.Text.Trim();

        foreach (var child in tabs.GetChildren()) { tabs.RemoveChild(child); child.QueueFree(); }
        foreach (var c in all)
        {
            int ready = Recipes().Count(p => CategoryOf(p) == c && Missing(p) == null);
            bool on = c == category && query == "";
            var tab = new Button { Text = ready > 0 ? $"{c} ({ready})" : c, Flat = !on, FocusMode = FocusModeEnum.None };
            tab.AddThemeFontSizeOverride("font_size", 15);
            tab.Modulate = new Color(1, 1, 1, on ? 1f : 0.65f);
            string chosen = c;
            tab.Pressed += () => { category = chosen; ClearSearch(); Refresh(); };
            tabs.AddChild(tab);
        }

        // A search looks through every category; otherwise the chosen tab, makeable ones first.
        Shown = (query == ""
                ? Recipes().Where(p => CategoryOf(p) == category)
                : Recipes().Where(p => Matches(p, query)))
            .OrderBy(p => Missing(p) == null ? 0 : 1).ThenBy(p => p.Tier).ToList();
        foreach (var child in grid.GetChildren()) { grid.RemoveChild(child); child.QueueFree(); }
        for (int i = 0; i < Shown.Count; i++) grid.AddChild(Card(Shown[i], i));
        if (Shown.Count == 0)
        {
            var none = Hud.Text(query == "" ? "Nothing here yet" : $"Nothing called \"{query}\"", 14);
            none.Modulate = new Color(1, 1, 1, 0.6f);
            grid.AddChild(none);
        }

        // Up to three rows of cards before it scrolls, never past the bottom of the screen.
        float rows = Mathf.Ceil(Math.Max(1, Shown.Count) / (float)Columns);
        float room = Math.Max(200f, GetViewportRect().Size.Y - 330f);
        scroll.CustomMinimumSize = new Vector2(grid.GetCombinedMinimumSize().X, Math.Min(Math.Min(rows, 3.3f) * (CardHeight + 10), room));
    }

    static bool Matches(ProcessDef p, string query) =>
        p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
        || (Items.IsKnown((ushort)p.Makes) && Items.Def(p.Makes).Name.Contains(query, StringComparison.OrdinalIgnoreCase))
        || p.Consumes.Any(i => Items.Def(i.Item).Name.Contains(query, StringComparison.OrdinalIgnoreCase));

    const float CardWidth = 168, CardHeight = 152;

    Control Card(ProcessDef def, int index)
    {
        string? missing = Missing(def);
        bool ready = missing == null;
        var card = new Button { FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(CardWidth, CardHeight) };
        var style = new StyleBoxFlat { BgColor = new Color(0.09f, 0.10f, 0.07f, 0.6f), BorderColor = new Color(Look.Cream, 0.22f) };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(10);
        var hover = (StyleBoxFlat)style.Duplicate();
        hover.BorderColor = new Color(Look.Cream, 0.6f);
        card.AddThemeStyleboxOverride("normal", style);
        card.AddThemeStyleboxOverride("hover", hover);
        card.AddThemeStyleboxOverride("pressed", hover);
        card.Modulate = new Color(1, 1, 1, ready ? 1f : 0.45f);
        card.TooltipText = Describe(def, missing);
        card.Pressed += () => Make(def);

        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        column.SetAnchorsPreset(LayoutPreset.FullRect);
        column.OffsetLeft = column.OffsetTop = 8;
        column.OffsetRight = column.OffsetBottom = -8;
        column.AddThemeConstantOverride("separation", 4);
        card.AddChild(column);

        // The result's picture, with the number key in the corner for the first nine.
        var picture = new Control { CustomMinimumSize = new Vector2(Picture, Picture), MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        // A fire gets a drawn flame; a recipe that sets something down without naming an item
        // shows its first input.
        picture.AddChild(def.Outcome == Outcome.Fire
            ? new FlameIcon { CustomMinimumSize = new Vector2(Picture, Picture), MouseFilter = MouseFilterEnum.Ignore }
            : Icon(Items.IsKnown((ushort)def.Makes) ? def.Makes : def.Consumes.Select(i => i.Item).FirstOrDefault(), Picture));
        if (def.MakesCount > 1) picture.AddChild(Corner($"×{def.MakesCount}", LayoutPreset.BottomRight));
        column.AddChild(picture);
        if (index < 9)
        {
            var key = Hud.Text((index + 1).ToString(), 12);
            key.Modulate = new Color(1, 1, 1, 0.45f);
            key.MouseFilter = MouseFilterEnum.Ignore;
            key.Position = new Vector2(8, 4);
            card.AddChild(key);
        }

        var name = Hud.Text(def.Name, 14);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        name.MouseFilter = MouseFilterEnum.Ignore;
        column.AddChild(name);

        // Inputs as pictures with have/need, then the tool and the hands-on mark.
        var needs = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        needs.AddThemeConstantOverride("separation", 4);
        foreach (var input in def.Consumes)
        {
            needs.AddChild(Icon(input.Item, 20));
            uint have = Have(input.Item);
            var n = Hud.Text($"{Math.Min(have, 99)}/{input.Count}", 12);
            if (have < input.Count) n.Modulate = new Color(1f, 0.75f, 0.45f);
            n.MouseFilter = MouseFilterEnum.Ignore;
            needs.AddChild(n);
        }
        if (def.NeedsTool != ItemTags.None && ToolFor(def.NeedsTool) is { } tool)
        {
            var spacer = new Control { CustomMinimumSize = new Vector2(4, 0), MouseFilter = MouseFilterEnum.Ignore };
            needs.AddChild(spacer);
            var toolIcon = Icon(tool, 20);
            // The tool is held, not used up: drawn in a ring.
            var ring = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            var ringStyle = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = new Color(Look.Cream, 0.5f) };
            ringStyle.SetBorderWidthAll(1);
            ringStyle.SetCornerRadiusAll(12);
            ring.AddThemeStyleboxOverride("panel", ringStyle);
            ring.AddChild(toolIcon);
            needs.AddChild(ring);
        }
        column.AddChild(needs);
        string note = HandsOn(def).Trim(' ', '·');
        if (note != "")
        {
            var hands = Hud.Text(note.Split('.')[0], 11);
            hands.HorizontalAlignment = HorizontalAlignment.Center;
            hands.Modulate = new Color(1f, 0.85f, 0.6f, 0.8f);
            hands.MouseFilter = MouseFilterEnum.Ignore;
            column.AddChild(hands);
        }
        return card;
    }

    static string Describe(ProcessDef def, string? missing)
    {
        var lines = new List<string> { def.Name };
        lines.Add("Takes " + string.Join(", ", def.Consumes.Select(i => $"{i.Count} {Items.Def(i.Item).Count(i.Count).ToLowerInvariant()}"))
            + (def.NeedsTool != ItemTags.None ? $", using {CraftingUi.ToolName(def.NeedsTool)}" : "") + $"  ·  about {def.Seconds:0}s");
        if (missing != null) lines.Add(missing);
        return string.Join("\n", lines);
    }

    // The simplest item that would do as a tool, for its picture.
    static Item? ToolFor(ItemTags tags) => Items.All.Values.Where(d => (d.Tags & tags) == tags)
        .OrderBy(d => Processes.List.FirstOrDefault(p => p.Makes == d.Id)?.Tier ?? 0).Select(d => (Item?)d.Id).FirstOrDefault();

    static TextureRect Icon(Item item, int size) => new()
    {
        Texture = ItemIcon.For(item),
        CustomMinimumSize = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    static Label Corner(string text, LayoutPreset where)
    {
        var l = Hud.Text(text, 12);
        l.SetAnchorsPreset(where);
        l.GrowHorizontal = GrowDirection.Begin;
        l.GrowVertical = GrowDirection.Begin;
        l.MouseFilter = MouseFilterEnum.Ignore;
        return l;
    }
}

// A small flame for recipes that make a fire rather than an item.
public partial class FlameIcon : Control
{
    public override void _Draw()
    {
        var c = Size / 2;
        float r = Mathf.Min(Size.X, Size.Y) * 0.36f;
        Vector2[] Flame(float scale, float lean) => Enumerable.Range(0, 24).Select(i =>
        {
            float t = i / 23f * Mathf.Tau;
            // A teardrop: round at the foot, drawn up to a point.
            float x = Mathf.Sin(t) * r * scale * (0.55f + 0.45f * Mathf.Cos(t * 0.5f) * Mathf.Cos(t * 0.5f));
            float y = -Mathf.Cos(t) * r * scale * 1.25f;
            return c + new Vector2(x + lean * (y < 0 ? -y / r : 0), y + r * 0.35f);
        }).ToArray();
        DrawColoredPolygon(Flame(1f, 0.08f), new Color(0.86f, 0.42f, 0.16f));
        DrawColoredPolygon(Flame(0.62f, 0.04f), new Color(0.98f, 0.76f, 0.32f));
        // Two crossed sticks under it.
        DrawLine(c + new Vector2(-r * 0.9f, r * 1.05f), c + new Vector2(r * 0.9f, r * 0.75f), Look.Bark, 4f, true);
        DrawLine(c + new Vector2(-r * 0.9f, r * 0.75f), c + new Vector2(r * 0.9f, r * 1.05f), Look.Bark, 4f, true);
    }
}
