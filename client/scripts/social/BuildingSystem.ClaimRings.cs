using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// bugfix-58 (Justin): laying out a claim marker shows the new claim's ring on the ground
// around the ghost, green where the land is free and red where it would overlap another
// claim or the clearing where newcomers wake, plus the rings of claims nearby, so a claim
// can be set right alongside a neighbour's. The rules are the server's (Claims.cs
// CheckStructureSite): claims may not overlap, counting markers still being built, and a
// marker stays out of the spawn clearing by a claim's width.
public partial class BuildingSystem
{
    const int ClaimRingSegments = 120;
    const float ClaimRingWidth = 0.3f;
    static readonly Color RingFree = new(0.62f, 0.95f, 0.55f, 0.75f);
    static readonly Color RingBlocked = new(1f, 0.42f, 0.32f, 0.8f);
    static readonly Color RingFriendly = new(0.97f, 0.90f, 0.70f, 0.55f);
    static readonly Color RingForeign = new(0.86f, 0.52f, 0.38f, 0.55f);
    static readonly Color RingSpawn = new(0.80f, 0.86f, 0.95f, 0.45f);

    readonly MeshInstance3D claimGhostRing = new() { CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Visible = false };
    readonly MeshInstance3D claimNeighbourRings = new() { CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Visible = false };
    readonly StandardMaterial3D claimRingMaterial = new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        VertexColorUseAsAlbedo = true,
        NoDepthTest = true,
        RenderPriority = 1,
    };
    // Each with the server's words for being too close to it, and whether it is a claim you belong to.
    readonly List<(float x, float z, string tooClose, bool friendly)> claimsNear = new();
    Vector3 claimsNearFrom = new(float.NaN, 0, 0);
    Vector3 claimRingFrom = new(float.NaN, 0, 0);

    partial void ClaimRingHook(Vector3? at, ref string? problem)
    {
        if (at is not { } p || Structures.Shape(placeKind).Model != "ClaimMarker")
        {
            claimGhostRing.Visible = claimNeighbourRings.Visible = false;
            claimsNearFrom = claimRingFrom = new Vector3(float.NaN, 0, 0);
            return;
        }
        if (claimGhostRing.GetParent() == null)
        {
            claimGhostRing.MaterialOverride = claimNeighbourRings.MaterialOverride = claimRingMaterial;
            root.AddChild(claimGhostRing);
            root.AddChild(claimNeighbourRings);
        }
        // The neighbours change rarely; gather and draw them again only when the ghost has moved a way.
        bool gathered = float.IsNaN(claimsNearFrom.X) || new Vector2(p.X - claimsNearFrom.X, p.Z - claimsNearFrom.Z).Length() > 4f;
        if (gathered) GatherClaimsNear(p);

        problem ??= ClaimSiteProblem(p.X, p.Z);
        // The ghost glides to a stop; the ring is only rebuilt while it moves.
        if (!gathered && !float.IsNaN(claimRingFrom.X) && new Vector2(p.X - claimRingFrom.X, p.Z - claimRingFrom.Z).Length() < 0.02f) return;
        claimRingFrom = p;
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        AddRing(st, p.X, p.Z, a => RingEdgeBlocked(p.X + MathF.Cos(a) * Social.ClaimRadius, p.Z + MathF.Sin(a) * Social.ClaimRadius) ? RingBlocked : RingFree);
        claimGhostRing.Mesh = st.Commit();
        claimGhostRing.Visible = true;
    }

    // Standing claims and claim markers still being built within reach of a new claim's edge.
    void GatherClaimsNear(Vector3 p)
    {
        claimsNearFrom = p;
        claimsNear.Clear();
        float reach = Social.ClaimRadius * 4f;
        if (conn() is { } c)
            foreach (var claim in c.Db.Claim.Iter())
                if (claim.ExpiresAt.MicrosecondsSinceUnixEpoch > SocialLayer.NowMicros
                    && new Vector2(claim.X - p.X, claim.Z - p.Z).Length() < reach)
                    claimsNear.Add((claim.X, claim.Z, $"Too close to {claim.OwnerName}'s claim", MayBar(claim.X, claim.Z)));
        foreach (var s in rows.Values)
            if (!s.Complete && Structures.Shape((int)s.Kind).Model == "ClaimMarker" && new Vector2(s.X - p.X, s.Z - p.Z).Length() < reach)
                claimsNear.Add((s.X, s.Z, "Too close to another claim marker being built", false));

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        foreach (var (x, z, _, friendly) in claimsNear)
            AddRing(st, x, z, _ => friendly ? RingFriendly : RingForeign);
        float spawnEdge = Social.SpawnClearRadius;
        if (new Vector2(p.X, p.Z).Length() < spawnEdge + reach)
            AddRing(st, 0f, 0f, _ => RingSpawn, spawnEdge);
        claimNeighbourRings.Mesh = st.Commit();
        claimNeighbourRings.Visible = true;
    }

    // Whether a point on the new claim's edge falls inside another claim or the spawn clearing.
    bool RingEdgeBlocked(float x, float z)
    {
        if (Social.InSpawnClearing(x, z)) return true;
        foreach (var (cx, cz, _, _) in claimsNear)
            if ((x - cx) * (x - cx) + (z - cz) * (z - cz) < Social.ClaimRadius * Social.ClaimRadius) return true;
        return false;
    }

    // The same rules and wording as the server, so the ghost turns red before a click is refused.
    string? ClaimSiteProblem(float x, float z)
    {
        float keepOut = Social.SpawnClearRadius + Social.ClaimRadius;
        if (x * x + z * z < keepOut * keepOut)
            return "This clearing is where newcomers wake up. Claim land further out";
        float minGap = Social.ClaimRadius * 2f;
        foreach (var (cx, cz, tooClose, _) in claimsNear)
            if ((x - cx) * (x - cx) + (z - cz) * (z - cz) < minGap * minGap)
                return tooClose;
        return null;
    }

    // A thin band hugging the ground along a circle, soft at both sides, coloured per segment.
    void AddRing(SurfaceTool st, float cx, float cz, Func<float, Color> colorAt, float radius = Social.ClaimRadius)
    {
        for (int i = 0; i < ClaimRingSegments; i++)
        {
            float a0 = i * Mathf.Tau / ClaimRingSegments, a1 = (i + 1) * Mathf.Tau / ClaimRingSegments;
            Color c0 = colorAt(a0), c1 = colorAt(a1);
            Vector3 Edge(float a, float r)
            {
                float x = cx + MathF.Cos(a) * r, z = cz + MathF.Sin(a) * r;
                return new Vector3(x, terrain.HeightAt(x, z) + 0.08f, z);
            }
            RingQuad(st, Edge(a0, radius - ClaimRingWidth), Edge(a1, radius - ClaimRingWidth), Edge(a1, radius), Edge(a0, radius), c0, c1, 0f, 1f);
            RingQuad(st, Edge(a0, radius), Edge(a1, radius), Edge(a1, radius + ClaimRingWidth), Edge(a0, radius + ClaimRingWidth), c0, c1, 1f, 0f);
        }
    }

    // a/d sit at the first angle, b/c at the second; alpha runs from the a-b side to the c-d side.
    static void RingQuad(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color c0, Color c1, float alphaAB, float alphaCD)
    {
        Color A = new(c0, c0.A * alphaAB), B = new(c1, c1.A * alphaAB), C = new(c1, c1.A * alphaCD), D = new(c0, c0.A * alphaCD);
        st.SetColor(A); st.AddVertex(a);
        st.SetColor(B); st.AddVertex(b);
        st.SetColor(C); st.AddVertex(c);
        st.SetColor(A); st.AddVertex(a);
        st.SetColor(C); st.AddVertex(c);
        st.SetColor(D); st.AddVertex(d);
    }
}
