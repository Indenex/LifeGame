// Journal coverage check (skills-22): every process the journal lists must be reachable
// through the "Things to try" hints and must train a skill. Run: dotnet run --project tools/JournalCheck
using LifeGame;
using LifeGame.Shared;

// NextSteps uses Recipes.Keys for key names; the check has no engine, so pass text through.
namespace LifeGame { public static class Recipes { internal static string Keys(string t) => t; } }

public static class JournalCheck
{
    // Ids that deliberately have no skill (kept short; say why).
    static readonly HashSet<string> NoSkill = new() { "claim.mark" };

    public static int Main()
    {
        var all = Skills.AllProcessIds().ToList();
        int bad = 0;

        foreach (var id in all)
            if (!NoSkill.Contains(id) && Skills.SpecFor(id) == null)
            { Console.WriteLine("no skill: " + id); bad++; }

        // A skill named on a ProcessDef must be a real one.
        foreach (var id in all)
            if (Processes.Find(id)?.Skill is { } named && Skills.Spec(named) == null)
            { Console.WriteLine($"{id} names unknown skill {named}"); bad++; }

        // Walk the hints the way a player would, taking the first, then the third, suggestion.
        foreach (int pick in new[] { 0, 2 })
        {
            var known = new HashSet<string>();
            while (true)
            {
                var next = NextSteps.Next(known).ToList();
                if (next.Count == 0) break;
                known.Add(next[Math.Min(pick, next.Count - 1)]);
            }
            foreach (var id in all)
                if (!known.Contains(id)) { Console.WriteLine($"unreachable (pick {pick}): {id}"); bad++; }
            Console.WriteLine($"pick {pick}: reached {known.Count} of {all.Count}");
        }

        Console.WriteLine(bad == 0 ? "JournalCheck OK" : $"JournalCheck FAILED ({bad})");
        return bad == 0 ? 0 : 1;
    }
}
