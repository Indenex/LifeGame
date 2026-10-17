using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// ui-85 (Justin: "make all ui windows and elements moveable by dragging"). Every window (anything
// framed as one: Ui.Window, or hand-framed with Ui.Frame) drags by its top edge at any time. HUD
// pieces (needs rings, sundial, hint corner, group window, the way arrow) drag while Alt is held,
// so ordinary clicks in play never move them. Things stay on screen, where you put them is saved
// in the local settings (section "layout"), and Options has "Reset layout".
public partial class Mover : Node
{
    // A HUD piece carries this meta, so the window scan leaves it to Alt-drag.
    public const string HudMark = "ui_hud_move";
    // The strip along a window's top that grabs it, and how close to the edge things may go.
    const float Grip = 34f, Margin = 6f;
    const string Section = "layout";

    sealed class Piece
    {
        public Control Node = null!;
        public string Key = "";
        public bool Hud;
        // The owner adds Offset(key) to its own position each frame (the way arrow).
        public bool OwnerPlaces;
        // Where it was put (saved), and how much of that is applied now (kept on screen).
        public Vector2 Wanted, Applied;
        public Vector2 LastPos, LastSize;
    }

    static readonly List<Piece> pieces = new();
    // --screenshot-moved (with --screenshot-friends): the Friends window shown moved up and right, not saved.
    static readonly bool Preview = OS.GetCmdlineUserArgs().Contains("--screenshot-moved");
    static readonly Dictionary<string, Vector2> applied = new();
    static Piece? dragging;

    // A HUD piece to move with Alt held. ownerPlaces: its owner sets its position every frame and
    // adds Offset(key) itself.
    public static void Hud(Control c, string key, bool ownerPlaces = false)
    {
        c.SetMeta(HudMark, true);
        Track(c, key, hud: true, ownerPlaces);
    }

    // How far a HUD piece placed by its owner has been moved (already kept on screen).
    public static Vector2 Offset(string key) => applied.GetValueOrDefault(key);

    // Options: everything back where it starts.
    public static void Reset()
    {
        Settings.Clear(Section);
        foreach (var p in pieces) p.Wanted = Vector2.Zero;
    }

    static void Track(Control c, string key, bool hud, bool ownerPlaces = false)
    {
        // A container lays its children out itself, so one inside it can't be moved.
        if (pieces.Any(p => p.Node == c) || c.GetParent() is Container) return;
        // Several windows from one owner each get their own place.
        string unique = key;
        for (int n = 2; pieces.Any(p => p.Key == unique); n++) unique = $"{key}#{n}";
        pieces.Add(new Piece
        {
            Node = c, Key = unique, Hud = hud, OwnerPlaces = ownerPlaces,
            Wanted = Preview && unique == "FriendsPanel" ? new Vector2(320, -150) : Settings.Get(Section, unique, Vector2.Zero),
            LastPos = Corner(c), LastSize = c.Size,
        });
    }

    public override void _EnterTree()
    {
        // The Esc menu and Options move too, whatever else is paused.
        ProcessMode = ProcessModeEnum.Always;
        GetTree().NodeAdded += Added;
    }
    public override void _ExitTree() => GetTree().NodeAdded -= Added;

    void Added(Node node)
    {
        // Survival's needs rings: the paper chip inside NeedsHud.
        if (node is NeedsHud needs)
        {
            Callable.From(() =>
            {
                if (IsInstanceValid(needs) && Find<PanelContainer>(needs) is { } chip) Hud(chip, "needs");
            }).CallDeferred();
            return;
        }
        if (node is not PanelContainer panel) return;
        // The window mark is set before the panel enters the tree, or by WarmLook a moment later.
        Callable.From(() =>
        {
            if (IsInstanceValid(panel) && panel.IsInsideTree() && panel.HasMeta(Ui.WindowMark) && !panel.HasMeta(HudMark))
                Track(panel, Owner(panel), hud: false);
        }).CallDeferred();
    }

    static T? Find<T>(Node n) where T : Node
    {
        foreach (var child in n.GetChildren())
            if (child is T t) return t;
            else if (Find<T>(child) is { } deeper) return deeper;
        return null;
    }

    // A window's name in the saved layout: the panel class it belongs to (FriendsPanel, TradeWindow).
    static string Owner(Node n)
    {
        for (Node? at = n; at != null; at = at.GetParent())
            if (at.GetType().Namespace == "LifeGame") return at.GetType().Name;
        return n.Name;
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left } mb)
        {
            if (!mb.Pressed)
            {
                if (dragging == null) return;
                Settings.Set(Section, dragging.Key, dragging.Wanted);
                dragging = null;
                Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
                GetViewport().SetInputAsHandled();
                return;
            }
            if (Input.MouseMode != Input.MouseModeEnum.Visible) return;
            if (Grab(mb.Position, mb.AltPressed) is not { } piece) return;
            dragging = piece;
            Input.SetDefaultCursorShape(Input.CursorShape.Move);
            GetViewport().SetInputAsHandled();
        }
        else if (e is InputEventMouseMotion motion && dragging != null)
        {
            dragging.Wanted += motion.Relative;
            Keep(dragging);
            GetViewport().SetInputAsHandled();
        }
    }

    // The topmost piece under the mouse that this press can pick up: a window by its top strip
    // (not over one of its buttons or fields), or with Alt held any HUD piece or window.
    Piece? Grab(Vector2 at, bool alt)
    {
        // Only plain parts of the strip grab: never a button, field, tab, slot or card under the mouse.
        var hovered = GetViewport().GuiGetHoveredControl();
        Piece? best = null;
        foreach (var p in pieces)
        {
            if (!IsInstanceValid(p.Node) || !Shown(p.Node)) continue;
            var rect = p.Node.GetGlobalRect();
            if (!rect.HasPoint(at)) continue;
            bool plain = hovered == null || hovered == p.Node || hovered is Label or Container or TextureRect or ColorRect or Panel;
            bool ok = alt || (!p.Hud && plain && at.Y <= rect.Position.Y + Grip);
            if (!ok) continue;
            if (best == null || Above(p.Node, best.Node)) best = p;
        }
        return best;
    }

    static bool Shown(Control c) => c.IsVisibleInTree() && c.Modulate.A > 0.2f && c.Size.X > 1f;

    static int LayerOf(Node n)
    {
        for (Node? at = n; at != null; at = at.GetParent())
            if (at is CanvasLayer layer) return layer.Layer;
        return 0;
    }

    static bool Above(Node a, Node b)
    {
        int la = LayerOf(a), lb = LayerOf(b);
        return la != lb ? la > lb : a.IsGreaterThan(b);
    }

    public override void _Process(double delta)
    {
        pieces.RemoveAll(p => !IsInstanceValid(p.Node));
        foreach (var p in pieces)
            if (p.Node.IsVisibleInTree()) Keep(p);
    }

    // Applies as much of where it was put as keeps it on screen. A window whose owner put it back
    // (same size, new spot) gets the move again; one that grew keeps it, as anchors carry it.
    void Keep(Piece p)
    {
        var c = p.Node;
        // Offsets, not position: a resized game window moves anchored things without moving them.
        if (!p.OwnerPlaces && p != dragging && c.Size == p.LastSize && Corner(c) != p.LastPos)
            p.Applied = Vector2.Zero;
        var view = GetViewport().GetVisibleRect().Size;
        var rect = c.GetGlobalRect();
        var home = rect.Position - p.Applied;
        var want = home + p.Wanted;
        var max = view - rect.Size - new Vector2(Margin, Margin);
        var to = new Vector2(
            Mathf.Clamp(want.X, Margin, Mathf.Max(Margin, max.X)),
            Mathf.Clamp(want.Y, Margin, Mathf.Max(Margin, max.Y)));
        var next = to - home;
        // A piece that starts half off screen by design (none today) is left where its owner put it.
        if (p.Wanted == Vector2.Zero) next = Vector2.Zero;
        if (next != p.Applied)
        {
            if (!p.OwnerPlaces) c.Position += next - p.Applied;
            p.Applied = next;
        }
        applied[p.Key] = p.Applied;
        p.LastPos = Corner(c);
        p.LastSize = c.Size;
    }

    static Vector2 Corner(Control c) => new(c.OffsetLeft, c.OffsetTop);
}
