using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Skill takes the tedium out of hands-on crafting: minigames lose steps as you
// improve, and a Master just does the work (D-047). Holding Shift while starting
// a craft lets a Master play the shortest version anyway, for a chance at better.
// Crafting calls these hooks; they are static so the minigame classes can ask too.
public partial class CraftingUi
{
    static partial void SkipMinigame(ProcessDef def, ref float? score)
    {
        if (Skills.SkipsMinigame(Journal.RankFor(def.Id)) && !Input.IsPhysicalKeyPressed(Key.Shift))
            score = Skills.MasterScore;
    }

    static partial void StepsFor(ProcessDef def, ref int steps) =>
        steps = Skills.MinigameSteps(steps, Journal.RankFor(def.Id));
}
