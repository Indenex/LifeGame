using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-9: named places. Walking into a named claim shows its name softly near the
// top of the screen for a few seconds, the way a map names a valley.
public partial class SocialLayer
{
    readonly Label arrival = new();
    ulong arrivedIn;
    Tween? arrivalTween;

    void ReadyPlaces()
    {
        var layer = new CanvasLayer { Layer = 2 };
        Hud.Soft(arrival, 26, 0.95f);
        arrival.HorizontalAlignment = HorizontalAlignment.Center;
        arrival.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        arrival.GrowHorizontal = Control.GrowDirection.Both;
        arrival.Position = new Vector2(0, 72);
        arrival.Modulate = new Color(1, 1, 1, 0);
        arrival.MouseFilter = Control.MouseFilterEnum.Ignore;
        layer.AddChild(arrival);
        AddChild(layer);
    }

    string? PlaceName(ulong claimId) => conn?.Db.ClaimName.ClaimId.Find(claimId)?.Name;

    // Called with the claim you stand in (or null) a few times a second.
    void ArriveAt(Claim? here)
    {
        ulong id = here?.Id ?? 0;
        if (id == arrivedIn) return;
        arrivedIn = id;
        if (here == null || PlaceName(here.Id) is not { } place || Panels.Open()) return;
        arrival.Text = place;
        arrivalTween?.Kill();
        arrivalTween = arrival.CreateTween();
        arrivalTween.TweenProperty(arrival, "modulate:a", 1f, 1.2f).SetTrans(Tween.TransitionType.Sine);
        arrivalTween.TweenInterval(2.5f);
        arrivalTween.TweenProperty(arrival, "modulate:a", 0f, 1.6f).SetTrans(Tween.TransitionType.Sine);
    }
}
