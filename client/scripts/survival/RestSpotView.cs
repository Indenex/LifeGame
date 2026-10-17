using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Waking places on the client (survival-23). Laying a sleeping mat (or pressing G on one you have
// laid) asks "Wake here after a fall?" with Set and Not now; Set tells the server (SetRespawn, which
// checks your right to the building, your reach and that the spot stands). Your waking place wears
// a small ring and "Your waking place" when you are near. The server's data is RestSpots
// (shared/RestSpots.cs); today the only rest spot is the sleeping mat, which House building lays.
public partial class RestSpotView : CanvasLayer
{
    // How close a laid mat counts as the one under your feet; matches House building's roll-up reach.
    const float OnMat = 1.1f;

    DbConnection? conn;
    Node world = null!;
    Terrain terrain = null!;
    Node3D player = null!;
    Hud hud = null!;
    PanelContainer panel = null!;
    Label body = null!;
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
                if (conn == null || MyMat() is not { } mat) return null;
                float d = new Vector2(mat.X - at.X, mat.Z - at.Z).Length();
                if (d >= OnMat) return null;
                return new UseOffer(IsMyPoint(mat) ? "Your waking place" : "Wake here after a fall?", d, () => Open(mat),
                    Target: new Vector3(mat.X, at.Y, mat.Z));
            },
        });
    }

    uint MyId() => conn?.Identity is { } id && conn.Db.Player.Identity.Find(id) is { } p ? p.PlayerId : 0;

    SleepingMat? MyMat() => MyId() is var me and not 0 && conn!.Db.SleepingMat.PlayerId.Find(me) is { } m ? m : null;

    RespawnPoint? MyPoint() => conn!.Db.MyRespawnPoint.Iter().Cast<RespawnPoint?>().FirstOrDefault();

    bool IsMyPoint(SleepingMat mat) =>
        MyPoint() is { } p && p.StructureId == mat.StructureId && Mathf.Abs(p.X - mat.X) < 0.01f && Mathf.Abs(p.Z - mat.Z) < 0.01f;

    void OnMatLaid(SleepingMat mat)
    {
        if (!ready || mat.PlayerId != MyId()) return;
        // Only the mat you just laid out beside you asks; one arriving from elsewhere is just a login.
        if (new Vector2(mat.X - player.Position.X, mat.Z - player.Position.Z).Length() > 2f) return;
        Open(mat);
    }

    void OnPoint(RespawnPoint point)
    {
        PlaceMarker(point);
        if (ready) hud.Note("You'll wake here.");
    }

    void Open(SleepingMat mat)
    {
        bool already = IsMyPoint(mat);
        Panels.Opening(this);
        body.Text = already
            ? "This is your waking place. If you fall, you wake here."
            : "If you fall, you wake here instead of at the edge of the wilderness.";
        setButton.Text = already ? "Wake here (set)" : "Set";
        setButton.Disabled = already;
        panel.Visible = true;
        (already ? rollButton : setButton).GrabFocus();
    }

    void Close() => panel.Visible = false;

    void Set()
    {
        if (conn != null && MyMat() is { } mat && RestSpots.OfItem(Item.SleepingMat) is { } spot)
            conn.Reducers.SetRespawn(spot.Id, mat.StructureId);
        Close();
    }

    void RollUp()
    {
        conn?.Reducers.RollUpMat();
        Close();
    }

    void BuildPanel()
    {
        var title = Ui.Title("Wake here after a fall?");
        body = Hud.Text("", 16);
        body.CustomMinimumSize = new Vector2(360, 0);
        body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        setButton = new Button { Text = "Set", CustomMinimumSize = new Vector2(110, 0) };
        setButton.Pressed += Set;
        var notNow = new Button { Text = "Not now", CustomMinimumSize = new Vector2(110, 0) };
        notNow.Pressed += Close;
        rollButton = new Button { Text = "Roll up the mat" };
        rollButton.Pressed += RollUp;
        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 10);
        buttons.AddChild(setButton);
        buttons.AddChild(notNow);
        buttons.AddChild(rollButton);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        column.AddChild(title);
        column.AddChild(body);
        column.AddChild(buttons);
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
