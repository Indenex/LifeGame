using LifeGame.Shared;

namespace LifeGame;

// The hands-on part of crafting is heard too: while a minigame is open (drilling,
// striking, shaping, coiling) the work sound for its process plays, and the job
// that follows carries it on (Atmosphere.Connect).
public partial class CraftingUi
{
    // Long enough that a drill keeps rasping however long the player takes; the
    // ember-nursing breaths belong to the job afterwards.
    const float MinigameSeconds = 600f;

    static partial void MinigameSound(ProcessDef def, bool playing) =>
        SoundScape.Current?.Working(playing ? SoundMix.WorkFor(def.Id) : Work.None, MinigameSeconds);
}
