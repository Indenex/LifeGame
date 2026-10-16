using Godot;
using LifeGame.Shared;

namespace LifeGame;

// claims-12: a marker that has stood a while looks lived-in. After a day, a cord is
// wound round the post; after three, more stones gather at its foot; after a week,
// notches are carved in the post and a small stone charm hangs from the bar.
public partial class ClaimMarker
{
    static Mesh? cordMesh, notchMesh, charmCordMesh, charmMesh, pebbleMesh;
    readonly Node3D postExtras = new();
    readonly Node3D groundExtras = new();
    long raisedAt = -1;
    int livedIn = -1;

    // The claim's raised time from the claim_age table (or -1 while unknown).
    public void SetRaisedAt(long micros)
    {
        raisedAt = micros;
        UpdateLivedIn();
    }

    void UpdateLivedIn()
    {
        if (claim == null) return;
        int level = raisedAt < 0 ? 0 : Social.LivedIn(raisedAt, SocialLayer.NowMicros);
        if (level == livedIn) return;
        livedIn = level;
        if (postExtras.GetParent() == null)
        {
            post.AddChild(postExtras);
            AddChild(groundExtras);
        }
        foreach (var child in postExtras.GetChildren()) child.QueueFree();
        foreach (var child in groundExtras.GetChildren()) child.QueueFree();

        cordMesh ??= Look.Faceted(new CylinderMesh { TopRadius = 0.072f, BottomRadius = 0.078f, Height = 0.035f, RadialSegments = 7 }, Look.LeafWarm.Darkened(0.15f));
        notchMesh ??= Look.Faceted(new CylinderMesh { TopRadius = 0.066f, BottomRadius = 0.066f, Height = 0.012f, RadialSegments = 6 }, Look.Bark.Darkened(0.35f));
        charmCordMesh ??= Look.Faceted(new CylinderMesh { TopRadius = 0.004f, BottomRadius = 0.004f, Height = 0.18f, RadialSegments = 4 }, Look.LeafWarm);
        charmMesh ??= Look.Faceted(new SphereMesh { Radius = 0.045f, Height = 0.07f, RadialSegments = 6, Rings = 3 }, Look.Rock.Lightened(0.15f));
        pebbleMesh ??= Look.Faceted(new SphereMesh { Radius = 0.13f, Height = 0.16f, RadialSegments = 6, Rings = 3 }, Look.Rock.Darkened(0.05f));

        if (level >= 1)
            for (int i = 0; i < 4; i++)
                postExtras.AddChild(new MeshInstance3D { Mesh = cordMesh, Position = new Vector3(0, 0.62f + i * 0.045f, 0), Rotation = new Vector3(0.08f * (i % 2 == 0 ? 1 : -1), 0, 0) });
        if (level >= 2)
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.Tau / 6f + 0.15f;
                float s = 0.75f + Terrain.Hash01(i, (int)claim.Id, 52) * 0.5f;
                groundExtras.AddChild(new MeshInstance3D
                {
                    Mesh = pebbleMesh,
                    Position = new Vector3(Mathf.Cos(a) * 0.46f, 0.03f, Mathf.Sin(a) * 0.46f),
                    Scale = new Vector3(s, s * 0.75f, s),
                    Rotation = new Vector3(0, a * 1.7f, 0),
                });
            }
        if (level >= 3)
        {
            for (int i = 0; i < 3; i++)
                postExtras.AddChild(new MeshInstance3D { Mesh = notchMesh, Position = new Vector3(0, 1.12f + i * 0.09f, 0) });
            postExtras.AddChild(new MeshInstance3D { Mesh = charmCordMesh, Position = new Vector3(-0.16f, 1.56f, 0) });
            postExtras.AddChild(new MeshInstance3D { Mesh = charmMesh, Position = new Vector3(-0.16f, 1.45f, 0) });
        }
    }
}
