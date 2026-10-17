using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-3: striking other players. The strike key itself belongs to Animals' Herd;
// Claims offers the nearest player in front when fighting would be allowed there.
// Being hurt shows as a soft red edge on the screen that fades as health returns.
// claims-27 (playtest notes, was claims-7 on the other account): a strike at nobody still
// swings the arm, and with a person close by a short note says why they weren't hit; a
// blow you take flushes red from the side it came from and names the striker.
public partial class SocialLayer
{
    readonly ColorRect hurtEdge = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    ShaderMaterial hurtMaterial = null!;
    float hurtShown;
    float markTimer;
    static readonly Color MarkedName = new(0.92f, 0.36f, 0.30f);
    // Set by Main: the same checks Herd uses for the strike key.
    public Func<bool> StrikeBlocked = () => false;
    public Func<Vector3> Facing = () => Vector3.Forward;
    double lastSwing;
    float hitShown;
    // Someone right beside you counts; only people well behind you are out of the cone.
    const float StrikeConeDot = -0.1f, StrikeAnyDirection = 1.0f;
    // How far past reach a person still earns a note when a strike misses them.
    const float MissNoteSlack = 2f;

    void ReadyFighting()
    {
        var layer = new CanvasLayer { Layer = 1 };
        hurtEdge.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        hurtMaterial = new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = @"shader_type canvas_item;
uniform float amount = 0.0;
uniform float hit = 0.0;
uniform vec2 hit_dir = vec2(0.0, 1.0);
void fragment() {
    vec2 d = UV - vec2(0.5);
    float edge = smoothstep(0.32, 0.75, length(d * vec2(1.25, 1.0)));
    // A blow flushes the edge on the side it came from.
    float side = smoothstep(0.05, 0.5, dot(d, hit_dir)) * smoothstep(0.18, 0.7, length(d * vec2(1.25, 1.0)));
    COLOR = vec4(0.45, 0.04, 0.03, max(edge * amount, side * hit * 0.75));
}",
            },
        };
        hurtEdge.Material = hurtMaterial;
        layer.AddChild(hurtEdge);
        AddChild(layer);
    }

    void AttachFighting()
    {
        // The struck player's flinch is a body pose now (Animation, anim-5 AvatarWork).
        Strike.Register("claims.player", OfferStrike);
        conn!.Db.StrikeEvent.OnInsert += (_, e) => { if (e.VictimId == myId) FeelHit(e); };
    }

    // The nearest other player in front of you within reach, if fighting is allowed
    // where you both stand and they aren't a protected newcomer.
    StrikeOffer? OfferStrike(Vector3 at, Vector3 facing)
    {
        if (conn == null || myId == 0 || !FightingAllowedAt(at.X, at.Z)) return null;
        long now = NowMicros;
        PlayerPos? best = null;
        float bestD = Social.StrikeReach;
        foreach (var p in conn.Db.PlayerPos.Iter())
        {
            if (p.PlayerId == myId) continue;
            var to = new Vector3(p.X - at.X, 0, p.Z - at.Z);
            float d = to.Length();
            if (d > bestD || d < 0.01f || OutOfCone(to, d, facing)) continue;
            if (!FightingAllowedAt(p.X, p.Z)) continue;
            if (conn.Db.PlayerStanding.PlayerId.Find(p.PlayerId) is { } h && h.GraceUntil.MicrosecondsSinceUnixEpoch > now) continue;
            if (conn.Db.Player.PlayerId.Find(p.PlayerId) is not { Online: true }) continue;
            best = p;
            bestD = d;
        }
        if (best is not { } target) return null;
        uint id = target.PlayerId;
        return new StrikeOffer(bestD, () => conn.Reducers.StrikePlayer(id));
    }

    static bool OutOfCone(Vector3 to, float d, Vector3 facing) => d > StrikeAnyDirection && (to / d).Dot(facing) < StrikeConeDot;

    bool FightingAllowedAt(float x, float z) =>
        !Social.InSpawnClearing(x, z) && (ClaimAt(x, z) is not { } c || c.AllowPvp);

    // Herd runs the strike itself. With nothing any lane offers, the arm still swings
    // (only you see it; the server shows real strikes to everyone), and a person close
    // by gets a note saying why they weren't hit.
    void StrikePressed()
    {
        if (conn == null || myId == 0 || StrikeBlocked()) return;
        double now = Time.GetTicksMsec() / 1000.0;
        if (now - lastSwing < 0.5) return;
        lastSwing = now;
        var facing = Facing();
        if (Strike.Best(myPos, facing) != null) return;
        var held = Me.Key(conn) is { } key ? Equipped.ItemOf(conn.Db.PlayerEquip.Owner.Find(key)) : null;
        if (held != Item.Bow) (avatarOf(myId) as Avatar)?.Act(WorkMotion.Strike, held);
        if (WhyNoStrike(facing) is { } why) hud.Note(why);
    }

    // The nearest person roughly ahead decides the note; nobody near, no note.
    string? WhyNoStrike(Vector3 facing)
    {
        PlayerPos? near = null;
        float nearD = Social.StrikeReach + MissNoteSlack;
        foreach (var p in conn!.Db.PlayerPos.Iter())
        {
            if (p.PlayerId == myId || conn.Db.Player.PlayerId.Find(p.PlayerId) is not { Online: true }) continue;
            var to = new Vector3(p.X - myPos.X, 0, p.Z - myPos.Z);
            float d = to.Length();
            if (d >= nearD || d < 0.01f || OutOfCone(to, d, facing)) continue;
            (near, nearD) = (p, d);
        }
        if (near is not { } who) return null;
        if (nearD > Social.StrikeReach) return "Too far away";
        if (!FightingAllowedAt(myPos.X, myPos.Z) || !FightingAllowedAt(who.X, who.Z)) return "No fighting here";
        if (conn.Db.PlayerStanding.PlayerId.Find(who.PlayerId) is { } h && h.GraceUntil.MicrosecondsSinceUnixEpoch > NowMicros)
            return $"{NameOf(who.PlayerId)} is new here. Leave them be for now";
        return null;
    }

    string NameOf(uint playerId) => conn?.Db.Player.PlayerId.Find(playerId)?.Name ?? "Someone";

    // You were struck: the screen flushes red from the striker's side and a note names them
    // (a killing blow leaves the words to the death card).
    void FeelHit(StrikeEvent e)
    {
        if (!e.Killed) hud.Note($"{NameOf(e.AttackerId)} struck you");
        if (conn?.Db.PlayerPos.PlayerId.Find(e.AttackerId) is not { } from) return;
        if (GetViewport().GetCamera3D() is not { } cam) return;
        // Where the striker stands, as a direction across the screen (y grows downward).
        var to = new Vector3(from.X - myPos.X, 0, from.Z - myPos.Z);
        var basis = cam.GlobalBasis;
        var right = new Vector3(basis.X.X, 0, basis.X.Z);
        var ahead = new Vector3(-basis.Z.X, 0, -basis.Z.Z);
        if (right.LengthSquared() < 1e-4f || ahead.LengthSquared() < 1e-4f) return;
        var dir = new Vector2(to.Dot(right.Normalized()), -to.Dot(ahead.Normalized()));
        if (dir.Length() < 0.01f) return;
        hurtMaterial.SetShaderParameter("hit_dir", dir.Normalized());
        hitShown = 1f;
    }

    float MyHealth()
    {
        if (conn?.Db.MyHealth.Iter().FirstOrDefault() is not { } h) return Social.MaxHealth;
        return Social.HealthAt(h.Health, h.AsOf.MicrosecondsSinceUnixEpoch, h.LastHitAt.MicrosecondsSinceUnixEpoch, NowMicros, h.Fed);
    }

    void ProcessFighting(float dt)
    {
        float missing = 1f - Mathf.Clamp(MyHealth() / Social.MaxHealth, 0f, 1f);
        // A quick flush when hit, settling to a faint edge that matches what is missing.
        float target = missing < 0.01f ? 0f : 0.15f + missing * 0.6f;
        hurtShown = Mathf.MoveToward(hurtShown, target, dt * (target > hurtShown ? 3f : 0.6f));
        hurtMaterial.SetShaderParameter("amount", hurtShown);
        hitShown = Mathf.MoveToward(hitShown, 0f, dt * 0.8f);
        hurtMaterial.SetShaderParameter("hit", hitShown);
        hurtEdge.Visible = hurtShown > 0.005f || hitShown > 0.005f;

        markTimer -= dt;
        if (markTimer <= 0f)
        {
            markTimer = 0.5f;
            TintMarkedNames();
        }
    }

    // Anyone who struck someone down in the last half hour shows a red name (D-053).
    void TintMarkedNames()
    {
        if (conn == null) return;
        long now = NowMicros;
        foreach (var p in conn.Db.PlayerPos.Iter())
        {
            if (p.PlayerId == myId || avatarOf(p.PlayerId) is not Avatar avatar) continue;
            bool marked = conn.Db.PlayerStanding.PlayerId.Find(p.PlayerId) is { } h && h.MarkedUntil.MicrosecondsSinceUnixEpoch > now;
            avatar.SetNameTint(marked ? MarkedName : Look.Cream);
        }
    }
}
