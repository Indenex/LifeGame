using System;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Batch devices on the client (crafting-23): plays the device's minigame and hands the
// score back, instead of starting a craft. The device's Use offers live in DeviceUse.cs.
public partial class CraftingUi
{
    Action<float>? deviceDone;

    // Plays the strike or kneading game for a process worked at a device. done gets the
    // score from 0 to 1; Esc abandons it and nothing is sent.
    public void PlayDeviceGame(ProcessDef process, Minigame kind, Action<float> done)
    {
        if (minigame != null || conn == null) return;
        var stand = new ProcessDef { Id = process.Id, Name = process.Name, Doing = process.Doing, Tier = process.Tier, Minigame = kind };
        minigame = kind == Minigame.Strike ? new StrikeGame(stand) : null;
        if (minigame == null) MinigameFor(stand, ref minigame);
        if (minigame == null) { done(Processes.DefaultScore); return; }
        deviceDone = done;
        reopenAfterMinigame = panel.Visible;
        panel.Visible = false;
        AddChild(minigame.Root);
        conn.Reducers.BeginMinigame(process.Id);
        MinigameSound(stand, true);
    }
}
