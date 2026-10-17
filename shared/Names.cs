// Player names (ui-31, Justin: "on character creation, people should get to choose
// their name"). Shared so the picker's field and the server's SetName agree on what is
// allowed (D-182: the server checks again, and also that nobody else has the name).
// Keep this file free of engine and SpacetimeDB types.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LifeGame.Shared
{
    public static class Names
    {
        public const int Min = 2, Max = 20;

        // Never a player's name: what the game calls you, staff words, and every town office.
        static readonly HashSet<string> Reserved = new(
            new[] { "you", "admin", "administrator", "moderator", "mod", "server", "system", "game", "everyone", "nobody", "someone" }
                .Concat(Towns.Offices.Select(o => o.Name.ToLowerInvariant())));

        // Spaces run together become one, the ends are trimmed, and control characters go.
        public static string Normalize(string raw)
        {
            var words = Social.StripControl(raw ?? "").Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words);
        }

        // The same name in any case (for "That name is taken").
        public static string Key(string name) => Normalize(name).ToLowerInvariant();

        static bool Mark(char c) => c is ' ' or '\'' or '’' or '-';

        // Why a (normalized) name can't be used, or null when it can. Uniqueness is the
        // server's to check (and the client's, from the player list, before asking).
        public static string? Problem(string name)
        {
            if (name.Length < Min) return $"A name needs at least {Min} letters";
            if (name.Length > Max) return $"A name can be at most {Max} characters";
            if (!name.All(c => char.IsLetter(c) || Mark(c))) return "Use letters, spaces, apostrophes and hyphens only";
            if (!char.IsLetter(name[0]) || !char.IsLetter(name[^1])) return "Start and end the name with a letter";
            for (int i = 1; i < name.Length; i++)
                if (Mark(name[i]) && Mark(name[i - 1])) return "Put a letter between spaces and marks";
            if (Reserved.Contains(name.ToLowerInvariant())) return "That name is kept for the game";
            return null;
        }

        // A name that fits the world, for the dice button: a given name, often with a
        // homely surname. Always valid by Problem.
        static readonly string[] Given =
        {
            "Bram", "Wren", "Fenna", "Rowan", "Hazel", "Tamsin", "Mira", "Orrin", "Pip", "Bell", "Elm", "Moss",
            "Fern", "Ash", "Briar", "Cob", "Dorran", "Ivy", "Juniper", "Kestrel", "Lark", "Merry", "Nell", "Perrin",
            "Quill", "Rook", "Sorrel", "Tansy", "Vale", "Willow", "Yarrow", "Alder", "Bryony", "Corin", "Dace", "Edda",
            "Finch", "Garrow", "Hollis", "Isla", "Jory", "Linnet", "Marlo", "Nim", "Odo", "Pell", "Rue", "Sedge", "Tam",
        };

        static readonly string[] Family =
        {
            "Thistledown", "Brook", "Hollow", "Fernhill", "Oakes", "Pebble", "Marsh", "Hearth", "Wick", "Thatch",
            "Reed", "Barrow", "Copse", "Dell", "Moor", "Heath", "Ford", "Lea", "Underhill", "Applewood", "Burrow",
            "Clay", "Millstone", "Rushby", "Willowmere", "Ashdown", "Bramblecot",
        };

        public static string Suggest(Random rng)
        {
            string given = Given[rng.Next(Given.Length)];
            if (rng.Next(3) == 0) return given;
            string full = $"{given} {Family[rng.Next(Family.Length)]}";
            return full.Length <= Max ? full : given;
        }

        // A suggestion nobody in `taken` (lower-case keys) has, if one turns up in a few tries.
        public static string Suggest(Random rng, ICollection<string> taken)
        {
            string name = Suggest(rng);
            for (int i = 0; i < 20 && taken.Contains(Key(name)); i++) name = Suggest(rng);
            return name;
        }
    }
}
