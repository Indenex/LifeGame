using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// claims-19: the town drawn from above as an ink map on parchment. Each claim is a circle of
// Social.ClaimRadius: the town's signed claims warm and filled, neighbours on no roster as
// muted dashed rings, and claims of other towns nearby in a cool tone. The map fits itself to
// what it shows; hovering names a claim and clicking one raises Picked. TownPanel fills it.
public partial class TownMap : Control
{
    public enum Kind { Signed, Free, OtherTown }

    // One claim on the map. Badges are short office marks ("M" for Mayor).
    public sealed record Spot(ulong ClaimId, float X, float Z, string Owner, uint OwnerId, Kind Kind, bool Mine, IReadOnlyList<string> Badges);

    // The look, kept here so Render and UX can retune it (D-240: parchment-and-ink warmth).
    static readonly Color Parchment = new(0.87f, 0.80f, 0.64f);
    static readonly Color ParchmentShade = new(0.74f, 0.64f, 0.47f);
    static readonly Color Ink = new(0.25f, 0.17f, 0.11f);
    static readonly Color SignedFill = new(0.83f, 0.55f, 0.30f, 0.80f);
    static readonly Color SignedEdge = new(0.45f, 0.26f, 0.13f);
    static readonly Color MineEdge = new(0.62f, 0.20f, 0.12f);
    static readonly Color FreeEdge = new(0.42f, 0.36f, 0.28f, 0.75f);
    static readonly Color OtherFill = new(0.42f, 0.53f, 0.55f, 0.45f);
    static readonly Color OtherEdge = new(0.25f, 0.36f, 0.40f);
    static readonly Color BadgeFill = new(0.96f, 0.90f, 0.74f);
    static readonly Color MayorBadgeFill = new(0.91f, 0.72f, 0.28f);
    static readonly Color SelectedRing = new(0.98f, 0.95f, 0.85f, 0.95f);
    static readonly Color Hover = new(0.98f, 0.95f, 0.85f, 0.55f);
    const float MarginMetres = 14f;
    const int NameSize = 13, BadgeSize = 10;

    public event Action<Spot>? Picked;
    public ulong Selected;
    // Shown faintly across the map while an office is being voted on ("Click a signer for Mayor").
    public string Prompt = "";

    List<Spot> spots = new();
    int hovered = -1;
    Vector2 centre;
    float scale = 1f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
        Resized += QueueRedraw;
    }

    public void SetSpots(List<Spot> claims)
    {
        spots = claims;
        hovered = Math.Min(hovered, spots.Count - 1);
        QueueRedraw();
    }

    // Fit every circle plus a margin into the control, keeping metres square. North (-Z) is up.
    void Fit()
    {
        if (spots.Count == 0) { centre = Vector2.Zero; scale = 1f; return; }
        float r = Social.ClaimRadius + MarginMetres;
        float minX = spots.Min(s => s.X) - r, maxX = spots.Max(s => s.X) + r;
        float minZ = spots.Min(s => s.Z) - r, maxZ = spots.Max(s => s.Z) + r;
        centre = new Vector2((minX + maxX) / 2f, (minZ + maxZ) / 2f);
        scale = Mathf.Min(Size.X / (maxX - minX), Size.Y / (maxZ - minZ));
    }

    Vector2 ToScreen(float x, float z) => Size / 2f + (new Vector2(x, z) - centre) * scale;

    int SpotAt(Vector2 at)
    {
        float r = Social.ClaimRadius * scale;
        int best = -1;
        float bestD = r;
        for (int i = 0; i < spots.Count; i++)
        {
            float d = ToScreen(spots[i].X, spots[i].Z).DistanceTo(at);
            if (d <= bestD) { best = i; bestD = d; }
        }
        return best;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion)
        {
            int at = SpotAt(motion.Position);
            if (at != hovered) { hovered = at; QueueRedraw(); }
            MouseDefaultCursorShape = at >= 0 ? CursorShape.PointingHand : CursorShape.Arrow;
        }
        else if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click)
        {
            int at = SpotAt(click.Position);
            if (at >= 0) Picked?.Invoke(spots[at]);
            AcceptEvent();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit && hovered >= 0) { hovered = -1; QueueRedraw(); }
    }

    public override void _Draw()
    {
        Fit();
        var paper = new StyleBoxFlat { BgColor = Parchment, BorderColor = ParchmentShade, AntiAliasing = true };
        paper.SetBorderWidthAll(3);
        paper.SetCornerRadiusAll(10);
        DrawStyleBox(paper, new Rect2(Vector2.Zero, Size));

        var font = GetThemeDefaultFont();
        float r = Social.ClaimRadius * scale;
        // Others first, so the town's own circles sit on top where rings overlap.
        foreach (var kind in new[] { Kind.Free, Kind.OtherTown, Kind.Signed })
            for (int i = 0; i < spots.Count; i++)
                if (spots[i].Kind == kind) DrawSpot(spots[i], i, r, font);

        if (Prompt != "")
            DrawString(font, new Vector2(0, Size.Y - 12), Prompt, HorizontalAlignment.Center, Size.X, NameSize, new Color(Ink, 0.85f));
        if (hovered >= 0 && hovered < spots.Count)
        {
            var s = spots[hovered];
            string tip = s.Kind switch { Kind.Free => $"{s.Owner}  ·  on no roster", Kind.OtherTown => $"{s.Owner}  ·  another town", _ => s.Owner };
            DrawString(font, new Vector2(0, 20), tip, HorizontalAlignment.Center, Size.X, NameSize + 1, Ink);
        }
    }

    void DrawSpot(Spot s, int index, float r, Font font)
    {
        var at = ToScreen(s.X, s.Z);
        switch (s.Kind)
        {
            case Kind.Signed:
                DrawCircle(at, r, SignedFill);
                DrawArc(at, r, 0, Mathf.Tau, 64, s.Mine ? MineEdge : SignedEdge, s.Mine ? 3f : 1.6f, true);
                break;
            case Kind.OtherTown:
                DrawCircle(at, r, OtherFill);
                DrawArc(at, r, 0, Mathf.Tau, 64, OtherEdge, 1.4f, true);
                break;
            default:
                DashedRing(at, r, FreeEdge);
                break;
        }
        if (s.ClaimId == Selected) DrawArc(at, r + 4f, 0, Mathf.Tau, 64, SelectedRing, 2.5f, true);
        else if (index == hovered) DrawArc(at, r + 3f, 0, Mathf.Tau, 64, Hover, 2f, true);

        // The marker itself, a small ink dot, and the owner's name beneath it.
        DrawCircle(at, Mathf.Max(2.5f, r * 0.08f), Ink);
        var nameColour = s.Kind == Kind.Free ? new Color(Ink, 0.65f) : Ink;
        string name = s.Mine ? $"{s.Owner} (you)" : s.Owner;
        DrawString(font, at + new Vector2(-r * 1.4f, NameSize + 6), name, HorizontalAlignment.Center, r * 2.8f, NameSize, nameColour);

        // Office badges along the top-right of the circle.
        for (int i = 0; i < s.Badges.Count; i++)
        {
            var b = at + Polar(r, Mathf.Pi / 4f - i * 0.5f);
            float br = Mathf.Max(7f, BadgeSize * 0.9f);
            DrawCircle(b, br, s.Badges[i] == MayorMark ? MayorBadgeFill : BadgeFill);
            DrawArc(b, br, 0, Mathf.Tau, 24, Ink, 1.2f, true);
            DrawString(font, b + new Vector2(-br, BadgeSize * 0.38f), s.Badges[i], HorizontalAlignment.Center, br * 2f, BadgeSize, Ink);
        }
    }

    // A point on the circle, angle 0 to the right, positive angles turning up (screen y is down).
    static Vector2 Polar(float r, float angle) => new(Mathf.Cos(angle) * r, -Mathf.Sin(angle) * r);

    void DashedRing(Vector2 at, float r, Color colour)
    {
        const int dashes = 24;
        float step = Mathf.Tau / dashes;
        for (int i = 0; i < dashes; i++)
            DrawArc(at, r, i * step, i * step + step * 0.55f, 6, colour, 1.6f, true);
    }

    // Short office marks: the first letter, or two where an earlier office already took the
    // letter (Mayor "M", Marshal "Ma"). Worked out from Towns.Offices, so new offices just work.
    public static readonly string MayorMark = Mark(Social.OfficeMayor);

    public static string Mark(byte office)
    {
        if (Towns.Get(office) is not { } def) return "?";
        bool taken = Towns.Offices.TakeWhile(o => o.Id != office).Any(o => o.Name[0] == def.Name[0]);
        return taken ? def.Name[..2] : def.Name[..1];
    }
}
