using System;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// A claim marker: a lashed wooden post on a little cairn, with a cloth that moves in
// the wind, and a faint ring on the ground that shows when you walk near the edge.
// As nobody tends it the post leans, the cloth fades and the ring thins.
public partial class ClaimMarker : Node3D
{
    static readonly Color Friendly = new(0.97f, 0.90f, 0.70f);
    static readonly Color Foreign = new(0.86f, 0.52f, 0.38f);
    static readonly Color ClothFresh = new(0.72f, 0.36f, 0.22f);
    static readonly Color ClothWorn = new(0.62f, 0.58f, 0.50f);
    static Mesh? postMesh, barMesh, stakeMesh, stoneMesh;

    readonly Terrain terrain;
    readonly Node3D post = new();
    readonly Node3D cloth = new();
    readonly MeshInstance3D clothMesh = new();
    readonly MeshInstance3D ring = new();
    readonly StandardMaterial3D clothMaterial = new()
    {
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        Roughness = 0.95f,
    };
    readonly StandardMaterial3D ringMaterial = new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        VertexColorUseAsAlbedo = true,
    };

    Claim claim = null!;
    bool friendly;
    float wear, ringAlpha, phase;
    bool falling;
    Vector3 lastRingBuiltFrom = new(float.NaN, 0, 0);

    public string OwnerName => claim.OwnerName;

    public ClaimMarker(Terrain ground)
    {
        terrain = ground;
        postMesh ??= Look.Faceted(new CylinderMesh { TopRadius = 0.06f, BottomRadius = 0.08f, Height = 1.9f, RadialSegments = 6 }, Look.Bark.Lightened(0.08f));
        barMesh ??= Look.Faceted(new CylinderMesh { TopRadius = 0.035f, BottomRadius = 0.035f, Height = 0.7f, RadialSegments = 5 }, Look.Bark.Lightened(0.12f));
        stakeMesh ??= Look.Faceted(new CylinderMesh { TopRadius = 0.015f, BottomRadius = 0.04f, Height = 0.55f, RadialSegments = 5 }, Look.Bark.Lightened(0.1f));
        stoneMesh ??= Look.Faceted(new SphereMesh { Radius = 0.2f, Height = 0.26f, RadialSegments = 6, Rings = 3 }, Look.Rock);

        // A small cairn of stones holds the post upright.
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.Tau / 5f + 0.4f;
            float s = 0.8f + Terrain.Hash01(i, 7, 41) * 0.5f;
            AddChild(new MeshInstance3D
            {
                Mesh = stoneMesh,
                Position = new Vector3(Mathf.Cos(a) * 0.24f, 0.05f, Mathf.Sin(a) * 0.24f),
                Scale = new Vector3(s, s * 0.8f, s),
                Rotation = new Vector3(0, a * 2.3f, 0),
            });
        }

        post.AddChild(new MeshInstance3D { Mesh = postMesh, Position = new Vector3(0, 0.95f, 0) });
        post.AddChild(new MeshInstance3D { Mesh = barMesh, Position = new Vector3(0.12f, 1.65f, 0), Rotation = new Vector3(0, 0, Mathf.Pi / 2f) });
        // Fiber lashing where the bar meets the post.
        post.AddChild(new MeshInstance3D
        {
            Mesh = Look.Faceted(new CylinderMesh { TopRadius = 0.085f, BottomRadius = 0.085f, Height = 0.08f, RadialSegments = 7 }, Look.LeafWarm),
            Position = new Vector3(0, 1.65f, 0),
        });
        AddChild(post);

        // The cloth hangs from the end of the bar and sways from its top edge.
        clothMesh.Mesh = new QuadMesh { Size = new Vector2(0.32f, 0.62f), CenterOffset = new Vector3(0, -0.31f, 0) };
        clothMesh.MaterialOverride = clothMaterial;
        cloth.Position = new Vector3(0.36f, 1.64f, 0);
        cloth.AddChild(clothMesh);
        post.AddChild(cloth);

        ring.MaterialOverride = ringMaterial;
        ring.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        ring.TopLevel = true;
        AddChild(ring);
    }

    public void Set(Claim c, bool mine)
    {
        bool moved = claim == null || claim.X != c.X || claim.Z != c.Z;
        claim = c;
        friendly = mine;
        Position = new Vector3(c.X, terrain.HeightAt(c.X, c.Z) - 0.03f, c.Z);
        if (moved)
        {
            Rotation = new Vector3(0, Terrain.Hash01((int)c.Id, 11, 5) * Mathf.Tau, 0);
            PlaceStakes();
        }
        lastRingBuiltFrom = new Vector3(float.NaN, 0, 0);
    }

    // Short stakes around the edge, so the boundary reads even without the ring.
    void PlaceStakes()
    {
        foreach (var child in GetChildren())
            if (child.HasMeta("stake")) child.QueueFree();
        const int count = 10;
        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.Tau / count;
            float x = claim.X + Mathf.Cos(a) * Social.ClaimRadius, z = claim.Z + Mathf.Sin(a) * Social.ClaimRadius;
            var stake = new MeshInstance3D
            {
                Mesh = stakeMesh,
                TopLevel = true,
                Position = new Vector3(x, terrain.HeightAt(x, z) + 0.22f, z),
                Rotation = new Vector3((Terrain.Hash01(i, (int)claim.Id, 3) - 0.5f) * 0.25f, a, (Terrain.Hash01(i, (int)claim.Id, 4) - 0.5f) * 0.25f),
            };
            stake.SetMeta("stake", true);
            AddChild(stake);
        }
    }

    // Called every frame with the local player's position: fades the ring in when
    // you are near the edge, and keeps the marker's look in step with its age.
    public void Near(Vector3 player)
    {
        if (claim == null || falling) return;
        float d = new Vector2(player.X - claim.X, player.Z - claim.Z).Length();
        float target = Mathf.Clamp(1f - Mathf.Abs(d - Social.ClaimRadius) / 7f, 0f, 1f);
        ringAlpha = Mathf.Lerp(ringAlpha, target, 0.05f);
        ring.Visible = ringAlpha > 0.01f;
        if (ring.Visible && float.IsNaN(lastRingBuiltFrom.X)) BuildRing();
    }

    public override void _Process(double delta)
    {
        if (claim == null || falling) return;
        long tended = claim.TendedAt.MicrosecondsSinceUnixEpoch;
        long life = (claim.ExpiresAt.MicrosecondsSinceUnixEpoch - tended) / 1_000_000;
        wear = Social.Wear(tended, life, SocialLayer.NowMicros);
        float weathered = Mathf.Clamp((wear - Social.WeatheredFrom) / (1f - Social.WeatheredFrom), 0f, 1f);

        phase += (float)delta;
        float gust = Mathf.Sin(phase * 1.3f) * 0.5f + Mathf.Sin(phase * 2.9f + 1.1f) * 0.25f;
        cloth.Rotation = new Vector3(gust * 0.22f * (1f - weathered * 0.6f), gust * 0.3f, 0);
        cloth.Scale = new Vector3(1f, 1f - weathered * 0.35f, 1f);
        post.Rotation = new Vector3(weathered * 0.22f, 0, weathered * 0.12f);
        clothMaterial.AlbedoColor = ClothFresh.Lerp(ClothWorn, weathered);

        var tint = friendly ? Friendly : Foreign;
        ringMaterial.AlbedoColor = new Color(tint, ringAlpha * (0.5f - weathered * 0.3f));
    }

    // A thin band that hugs the ground along the claim's edge, soft at both sides.
    void BuildRing()
    {
        lastRingBuiltFrom = new Vector3(claim.X, 0, claim.Z);
        const int segments = 120;
        const float width = 0.35f;
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * Mathf.Tau / segments, a1 = (i + 1) * Mathf.Tau / segments;
            var inner0 = Edge(a0, Social.ClaimRadius - width);
            var mid0 = Edge(a0, Social.ClaimRadius);
            var outer0 = Edge(a0, Social.ClaimRadius + width);
            var inner1 = Edge(a1, Social.ClaimRadius - width);
            var mid1 = Edge(a1, Social.ClaimRadius);
            var outer1 = Edge(a1, Social.ClaimRadius + width);
            Quad(st, inner0, inner1, mid1, mid0, 0f, 1f);
            Quad(st, mid0, mid1, outer1, outer0, 1f, 0f);
        }
        ring.Mesh = st.Commit();
    }

    Vector3 Edge(float angle, float radius)
    {
        float x = claim.X + Mathf.Cos(angle) * radius, z = claim.Z + Mathf.Sin(angle) * radius;
        return new Vector3(x, terrain.HeightAt(x, z) + 0.06f, z);
    }

    static void Quad(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float alphaAB, float alphaCD)
    {
        var ca = new Color(1, 1, 1, alphaAB);
        var cc = new Color(1, 1, 1, alphaCD);
        st.SetColor(ca); st.AddVertex(a);
        st.SetColor(ca); st.AddVertex(b);
        st.SetColor(cc); st.AddVertex(c);
        st.SetColor(ca); st.AddVertex(a);
        st.SetColor(cc); st.AddVertex(c);
        st.SetColor(cc); st.AddVertex(d);
    }

    // The marker topples and sinks away instead of vanishing.
    public void Fall()
    {
        falling = true;
        ring.Visible = false;
        var tween = CreateTween();
        tween.TweenProperty(post, "rotation:x", 1.45f, 1.4f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenInterval(1.5f);
        tween.TweenProperty(this, "position:y", Position.Y - 0.6f, 2.5f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
