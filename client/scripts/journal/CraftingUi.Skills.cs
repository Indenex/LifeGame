using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Skill takes the tedium out of hands-on crafting: minigames lose steps as you
// improve, and a Master just does the work (D-047). Holding the
// "craft_play_minigame" action (Ctrl, bound by UX/UI in Controls.cs; Journal.cs
// binds it only if missing) while starting a craft lets a Master play the shortest version anyway.
// Crafting calls these hooks; they are static so the minigame classes can ask too.
public partial class CraftingUi
{
    public const string PlayMinigame = "craft_play_minigame";

    static partial void SkipMinigame(ProcessDef def, ref float? score)
    {
        if (Skills.SkipsMinigame(Journal.RankFor(def.Id)) && !Input.IsActionPressed(PlayMinigame))
            score = Skills.MasterScore;
    }

    static partial void Mastered(ProcessDef def, ref bool mastered) =>
        mastered = Skills.SkipsMinigame(Journal.RankFor(def.Id));

    static partial void StepsFor(ProcessDef def, ref int steps) =>
        steps = Skills.MinigameSteps(steps, Journal.RankFor(def.Id));
}
