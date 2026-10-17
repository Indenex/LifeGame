using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Waking places on the client (survival-23). Laying a sleeping mat (or pressing G on one you have
// laid) asks "Wake here after a fall?" with Set and Not now; Set tells the server (SetRespawn, which
// checks your right to the building, your reach and that the spot stands). Your waking place wears
// a small ring and "Your waking place" when you are near. The server's data is RestSpots
// (shared/RestSpots.cs): the sleeping mat, which House building lays, and the straw bed it builds.
public partial class RestSpotView : CanvasLayer
{
    // How close a laid mat counts as the one under your feet; matches House building's roll-up reach.
    const float OnMat = 1.1f;
    // How close a built rest spot (the straw bed) must be to be offered; the server checks reach too.
    const float OnBed = 1.6f;

    // The rest spot the window is about: a laid mat or a built bed.
    record Spot(RestSpotDef Def, ulong StructureId, float X, float Z);
    Spot? current;

    DbConnection? conn;
    Node world = null!;
    Terrain terrain = null!;
    Node3D player = null!;
    Hud hud = null!;
    PanelContainer panel = null!;
    Label title = null!, body = null!, hint = null!;
    Button setButton = null!;
    Button rollButton = null!;
    Node3D? marker;
    Label3D? markerLabel;
    // Set once the first subscription burst is over, so mats and waking places already there
    // at login don't pop the prompt or say "You'll wake here".
    bool ready;
    float readyIn = 3f;

    public void Setup(Node worldRoot, Terrain terrainNode, Node3D playerNode, Hud hudNode)
    {
        world = worldRoot;
        terrain = terrainNode;
        player = playerNode;
        hud = hudNode;
        Layer = 5;
        BuildPanel();
        AddChild(panel);
        panel.Visible = false;
    }

    public static string[] Queries => new[] { "SELECT * FROM my_respawn_point" };

    public void Attach(DbConnection connection)
    {
        conn = connection;
        conn.Db.SleepingMat.OnInsert += (_, m) => OnMatLaid(m);
        conn.Db.SleepingMat.OnDelete += (_, m) => { if (m.PlayerId == MyId()) panel.Visible = false; };
        conn.Db.MyRespawnPoint.OnInsert += (_, r) => OnPoint(r);
        conn.Db.MyRespawnPoint.OnUpdate += (_, _, r) => OnPoint(r);
        conn.Db.MyRespawnPoint.OnDelete += (_, _) => ClearMarker();
        Use.Register(new UseHandler
        {
            Id = "survival.rest_spot",
            // Above "Roll up your mat" (29), which moves into the prompt.
            Priority = 30,
            Find = at =>
            {
                if (conn == null || Nearest(at) is not { } near) return null;
                var (spot, d) = near;
                return new UseOffer(IsMyPoint(spot) ? "Your waking place" : "Wake here after a fall?", d, () => Open(spot),
                    Target: new Vector3(spot.X, at.Y, spot.Z));
            },
        });
    }

    uint MyId() => Me.PlayerId(conn);

    SleepingMat? MyMat() => MyId() is var me and not 0 && conn!.Db.SleepingMat.PlayerId.Find(me) is { } m ? m : null;

    RespawnPoint? MyPoint() => conn!.Db.MyRespawnPoint.Iter().Cast<RespawnPoint?>().FirstOrDefault();

    bool IsMyPoint(Spot spot) =>
        MyPoint() is { } p && p.Spot == spot.Def.Id && p.StructureId == spot.StructureId && Mathf.Abs(p.X - spot.X) < 0.01f && Mathf.Abs(p.Z - spot.Z) < 0.01f;

    Spot? OfMat(SleepingMat mat) =>
        RestSpots.OfItem(Item.SleepingMat) is { } def ? new Spot(def, mat.StructureId, mat.X, mat.Z) : null;

    // The closest rest spot in reach of a point: your laid mat, or a finished building whose kind is a
    // RestSpotDef (shared/RestSpots.cs), so a new bed is only a data entry there.
    (Spot, float)? Nearest(Vector3 at)
    {
        (Spot, float)? best = null;
        if (MyMat() is { } mat && OfMat(mat) is { } m)
        {
            float d = new Vector2(m.X - at.X, m.Z - at.Z).Length();
            if (d < OnMat) best = (m, d);
        }
        foreach (var s in conn!.Db.Structure.Iter())
        {
            if (!s.Complete || RestSpots.OfStructure((int)s.Kind) is not { } def) continue;
            float d = new Vector2(s.X - at.X, s.Z - at.Z).Length();
            if (d < OnBed && (best is not { } b || d < b.Item2)) best = (new Spot(def, s.Id, s.X, s.Z), d);
        }
        return best;
    }

    void OnMatLaid(SleepingMat mat)
    {
        if (!ready || mat.PlayerId != MyId()) return;
        // Only the mat you just laid out beside you asks; one arriving from elsewhere is just a login.
        if (new Vector2(mat.X - player.Position.X, mat.Z - player.Position.Z).Length() > 2f) return;
        if (OfMat(mat) is { } spot) Open(spot);
    }

    void OnPoint(RespawnPoint point)
    {
        PlaceMarker(point);
        if (ready) hud.Note("You'll wake here.");
    }

    void Open(Spot spot)
    {
        current = spot;
        bool already = IsMyPoint(spot);
        rollButton.Visible = spot.Def.Item == Item.SleepingMat;
        Panels.Opening(this);
        title.Text = already ? "Your waking place" : "Wake here after a fall?";
        body.Text = already
            ? "If you fall, you wake here."
            : "If you fall, you wake here instead of at the edge of the wilderness.";
        // Once it's set there's nothing to choose, only the mat to roll up.
        setButton.Visible = !already;
        hint.Text = already ? "Esc closes" : "Enter wakes here    Esc not now";
        panel.Visible = true;
    }

    void Close() => panel.Visible = false;

    void Set()
    {
        if (conn != null && current is { } spot) conn.Reducers.SetRespawn(spot.Def.Id, spot.StructureId);
        Close();
    }

    // Lie down on the mat (survival-27, Justin's pick): just a pose everyone sees; walking off
    // or StandUp ends it. The server checks the same things as SetRespawn.
    void LieDown()
    {
        if (conn != null && current is { } spot) conn.Reducers.LieOnRestSpot(spot.Def.Id, spot.StructureId);
        Close();
    }

    void RollUp()
    {
        conn?.Reducers.RollUpMat();
        Close();
    }

    // The shared frame (ui-37): the title, a soft line, one filled button for the choice and
    // quiet text buttons for the rest, and the keys underneath like every other window.
    void BuildPanel()
    {
        title = Ui.Title("");
        body = Hud.Text("", 15);
        body.Modulate = new Color(1, 1, 1, 0.8f);
        body.CustomMinimumSize = new Vector2(380, 0);
        body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        setButton = Ui.MainButton("Wake here", Set);
        var notNow = ControlsPage.TextButton("Not now", Close);
        var lieButton = ControlsPage.TextButton("Lie down", LieDown);
        rollButton = ControlsPage.TextButton("Roll up the mat", RollUp);
        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 18);
        buttons.AddChild(setButton);
        buttons.AddChild(notNow);
        buttons.AddChild(lieButton);
        buttons.AddChild(rollButton);
        hint = Hud.Text("", 13);
        hint.Modulate = new Color(1, 1, 1, 0.55f);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        column.AddChild(title);
        column.AddChild(body);
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 2) });
        column.AddChild(buttons);
        column.AddChild(hint);
        panel = Ui.Window(column);
        panel.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Begin;
        panel.OffsetBottom = -150;
        Panels.Centre(this, Close);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!panel.Visible || e is not InputEventKey { Pressed: true, Echo: false }) return;
        if (e.IsActionPressed("ui_cancel")) Close();
        else if (e is InputEventKey { PhysicalKeycode: Key.Enter or Key.KpEnter } && setButton.Visible) Set();
        else return;
        GetViewport().SetInputAsHandled();
    }

    // --- The mark on your waking place ---------------------------------------

    void PlaceMarker(RespawnPoint point)
    {
        ClearMarker();
        marker = new Node3D { Position = new Vector3(point.X, terrain.HeightAt(point.X, point.Z) + 0.06f, point.Z) };
        var ring = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.55f, OuterRadius = 0.62f, Rings = 24, RingSegments = 6 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.95f, 0.82f, 0.45f, 0.55f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
        };
        marker.AddChild(ring);
        markerLabel = new Label3D
        {
            Text = "Your waking place",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.004f,
            FontSize = 30,
            OutlineSize = 8,
            OutlineModulate = new Color(0.08f, 0.09f, 0.07f, 0.6f),
            Position = new Vector3(0, 0.9f, 0),
            Modulate = new Color(1f, 0.93f, 0.7f, 0f),
            NoDepthTest = true,
        };
        marker.AddChild(markerLabel);
        world.AddChild(marker);
    }

    void ClearMarker()
    {
        marker?.QueueFree();
        marker = null;
        markerLabel = null;
    }

    public override void _Process(double delta)
    {
        if (!ready && (readyIn -= (float)delta) <= 0f) ready = true;
        if (marker == null || markerLabel == null || player == null) return;
        // The label shows when you are near, fading in over the last few metres.
        float d = new Vector2(marker.Position.X - player.Position.X, marker.Position.Z - player.Position.Z).Length();
        float a = Mathf.Clamp((7f - d) / 3f, 0f, 1f);
        markerLabel.Modulate = new Color(1f, 0.93f, 0.7f, a);
    }
}
