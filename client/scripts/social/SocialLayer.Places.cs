using System.Collections.Generic;
using System.Linq;
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
        ReadyArrivalPreview();
    }

    string? PlaceName(ulong claimId) => conn?.Db.ClaimName.ClaimId.Find(claimId)?.Name;

    // claims-34: walking into a town's land names the town ("Entering Willow Run"), and walking out of it
    // says so more quietly. Each town speaks at most once a minute, so stepping over the edge and back
    // stays calm. Moving between claims of the same town says nothing.
    const double TownNoteCooldown = 60.0;
    readonly Dictionary<ulong, double> townNoted = new();
    ulong arrivedTown;
    string arrivedTownName = "";

    void ReadyArrivalPreview()
    {
        // --screenshot-entering: the note on screen for Render, with no server.
        if (OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--screenshot-entering")))
            GetTree().CreateTimer(1.5).Timeout += () => ShowArrival("Entering Willow Run", 0.95f);
    }

    // Called with the claim you stand in (or null) a few times a second.
    void ArriveAt(Claim? here)
    {
        ulong id = here?.Id ?? 0;
        if (id == arrivedIn) return;
        arrivedIn = id;
        ulong prevTown = arrivedTown;
        string prevName = arrivedTownName;
        var townHere = here == null || conn == null ? null
            : conn.Db.TownSigner.ClaimId.Find(here.Id) is { } signer ? conn.Db.Town.Id.Find(signer.TownId) : null;
        arrivedTown = townHere?.Id ?? 0;
        arrivedTownName = townHere?.Name ?? "";
        if (Panels.Open()) return;
        string? place = here == null ? null : PlaceName(here.Id);
        if (arrivedTown != 0 && arrivedTown != prevTown && TownNoteDue(arrivedTown))
            ShowArrival(place == null ? $"Entering {arrivedTownName}" : $"Entering {arrivedTownName}\n{place}", 0.95f);
        else if (place != null) ShowArrival(place, 0.95f);
        else if (prevTown != 0 && arrivedTown != prevTown && TownNoteDue(prevTown))
            ShowArrival($"Leaving {prevName}", 0.6f);
    }

    bool TownNoteDue(ulong townId)
    {
        double now = Time.GetTicksMsec() / 1000.0;
        if (townNoted.TryGetValue(townId, out var last) && now - last < TownNoteCooldown) return false;
        townNoted[townId] = now;
        return true;
    }

    void ShowArrival(string text, float strength)
    {
        arrival.Text = text;
        arrivalTween?.Kill();
        arrivalTween = arrival.CreateTween();
        arrivalTween.TweenProperty(arrival, "modulate:a", strength, 1.2f).SetTrans(Tween.TransitionType.Sine);
        arrivalTween.TweenInterval(2.5f);
        arrivalTween.TweenProperty(arrival, "modulate:a", 0f, 1.6f).SetTrans(Tween.TransitionType.Sine);
    }
}
