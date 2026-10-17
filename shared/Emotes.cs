// Emotes (Animation lane, anim-16): gestures a player makes on request, seen by everyone
// through the PlayerAct event. Shared by the server module (which checks the request) and
// the client (which offers and plays them). A new emote is an entry here plus its pose in
// client/scripts/animation/WorkMotion.cs.
using System.Collections.Generic;
using System.Linq;

namespace LifeGame.Shared
{
    public sealed class EmoteDef
    {
        public WorkMotion Motion;
        public string Name = "";
    }

    public static class Emotes
    {
        public static readonly IReadOnlyList<EmoteDef> All = new[]
        {
            new EmoteDef { Motion = WorkMotion.Wave, Name = "Wave" },
            new EmoteDef { Motion = WorkMotion.Cheer, Name = "Cheer" },
            new EmoteDef { Motion = WorkMotion.Dance, Name = "Dance" },
            new EmoteDef { Motion = WorkMotion.Bow, Name = "Bow" },
            new EmoteDef { Motion = WorkMotion.Clap, Name = "Clap" },
            new EmoteDef { Motion = WorkMotion.Shrug, Name = "Shrug" },
            new EmoteDef { Motion = WorkMotion.Point, Name = "Point" },
        };

        static readonly HashSet<WorkMotion> motions = All.Select(e => e.Motion).ToHashSet();

        public static bool IsEmote(WorkMotion m) => motions.Contains(m);

        // The shortest gap between two emotes from one player, in microseconds.
        public const long CooldownMicros = 1_500_000;
    }
}
