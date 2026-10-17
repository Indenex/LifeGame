using LifeGame.Shared;

namespace LifeGame;

// Swimming (swim-2): the player's Swimming rank eases the slow-down of wading and floating
// (Wading.SpeedFactor). The rank comes from the skill row the server keeps.
public partial class Main
{
    int SwimmingRank()
    {
        if (conn == null) return 0;
        foreach (var skill in conn.Db.MySkills.Iter())
            if (skill.Spec == "swimming") return Skills.Rank(skill.Points);
        return 0;
    }
}
