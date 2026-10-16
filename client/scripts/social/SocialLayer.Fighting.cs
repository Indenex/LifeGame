using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-3: striking other players. The strike key itself belongs to Animals' Herd;
// Claims offers the nearest player in front when fighting would be allowed there.
// Being hurt shows as a soft red edge on the screen that fades as health returns.
public partial class SocialLayer
{
    const float FlinchSeconds = 0.25f;

    readonly ColorRect hurtEdge = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    ShaderMaterial hurtMaterial = null!;
    float hurtShown;
    float markTimer;
    static readonly Color MarkedName = new(0.92f, 0.36f, 0.30f);

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
void fragment() {
    vec2 d = UV - vec2(0.5);
    float edge = smoothstep(0.32, 0.75, length(d * vec2(1.25, 1.0)));
    COLOR = vec4(0.45, 0.04, 0.03, edge * amount);
}",
            },
        };
        hurtEdge.Material = hurtMaterial;
        layer.AddChild(hurtEdge);
        AddChild(layer);
    }

    void AttachFighting()
    {
        conn!.Db.StrikeEvent.OnInsert += (_, e) => OnStrike(e);
        Strike.Register("claims.player", OfferStrike);
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
            if (d > bestD || d < 0.01f || to / d is var dir && dir.Dot(facing) < 0.4f) continue;
            if (!FightingAllowedAt(p.X, p.Z)) continue;
            if (conn.Db.PlayerHealth.PlayerId.Find(p.PlayerId) is { } h && h.GraceUntil.MicrosecondsSinceUnixEpoch > now) continue;
            if (conn.Db.Player.PlayerId.Find(p.PlayerId) is not { Online: true }) continue;
            best = p;
            bestD = d;
        }
        if (best is not { } target) return null;
        uint id = target.PlayerId;
        return new StrikeOffer(bestD, () => conn.Reducers.StrikePlayer(id));
    }

    bool FightingAllowedAt(float x, float z) =>
        !Social.InSpawnClearing(x, z) && (ClaimAt(x, z) is not { } c || c.AllowPvp);

    // The struck player flinches on everyone's screen.
    void OnStrike(StrikeEvent e)
    {
        if (avatarOf(e.VictimId) is not { } body) return;
        var tween = body.CreateTween();
        tween.TweenProperty(body, "scale", new Vector3(1.08f, 0.9f, 1.08f), FlinchSeconds * 0.35f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(body, "scale", Vector3.One, FlinchSeconds * 0.65f).SetTrans(Tween.TransitionType.Sine);
    }

    float MyHealth()
    {
        if (conn?.Db.PlayerHealth.PlayerId.Find(myId) is not { } h) return Social.MaxHealth;
        return Social.HealthAt(h.Health, h.AsOf.MicrosecondsSinceUnixEpoch, h.LastHitAt.MicrosecondsSinceUnixEpoch, NowMicros, h.Fed);
    }

    void ProcessFighting(float dt)
    {
        float missing = 1f - Mathf.Clamp(MyHealth() / Social.MaxHealth, 0f, 1f);
        // A quick flush when hit, settling to a faint edge that matches what is missing.
        float target = missing < 0.01f ? 0f : 0.15f + missing * 0.6f;
        hurtShown = Mathf.MoveToward(hurtShown, target, dt * (target > hurtShown ? 3f : 0.6f));
        hurtMaterial.SetShaderParameter("amount", hurtShown);
        hurtEdge.Visible = hurtShown > 0.005f;

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
            bool marked = conn.Db.PlayerHealth.PlayerId.Find(p.PlayerId) is { } h && h.MarkedUntil.MicrosecondsSinceUnixEpoch > now;
            avatar.SetNameTint(marked ? MarkedName : Look.Cream);
        }
    }
}
