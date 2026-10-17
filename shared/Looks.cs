// What a player can look like, chosen by the player and stored on the server
// (model-3, D-233; races model-6, D-249/D-255). Shared by the server module and
// the client so both agree on what is allowed. Keep this file free of engine and
// SpacetimeDB types.
namespace LifeGame.Shared
{
    // One choice in the picker (a colour or a part), with how many there are and,
    // for parts, their names. Colours themselves live in the client (ChibiFigure).
    public sealed class LookOption
    {
        public readonly string Name;
        public readonly int Count;
        public readonly string[]? Names;

        public LookOption(string name, int count, string[]? names = null)
        {
            Name = name;
            Count = names?.Length ?? count;
            Names = names;
        }
    }

    // A race the player can be, and its choices in the order of player_look's four
    // option bytes. Races and their options are append-only: a stored look keeps
    // meaning the same thing.
    public sealed class LookRace
    {
        public readonly string Name;
        public readonly LookOption[] Options;

        public LookRace(string name, params LookOption[] options)
        {
            Name = name;
            Options = options;
        }
    }

    public static class Looks
    {
        // How many skin tones and hair colours there are. The colours themselves
        // live in the client (HumanFigure).
        public const int Skins = 5, Hairs = 5;

        // Names for the picker, in the order of the cut and beard numbers.
        public static readonly string[] CutNames = { "Locks", "Cropped", "Long", "Tied back" };
        public static readonly string[] BeardNames = { "None", "Short" };

        // The roster (D-255: Bramble and Pebble for now), by race byte.
        public const byte Bramble = 0, Pebble = 1;
        public const int OptionSlots = 4;
        public static readonly LookRace[] Races =
        {
            new("Bramble", new LookOption("Skin", Skins), new LookOption("Hair", Hairs), new LookOption("Cut", 0, new[] { "Tousled", "Cropped" })),
            new("Pebble", new LookOption("Skin", 3), new LookOption("Shell", 3), new LookOption("Pattern", 0, new[] { "Plain", "Scutes" })),
        };

        // True when every choice is one the game has.
        public static bool Valid(byte skin, byte hair, byte cut, byte beard) =>
            skin < Skins && hair < Hairs && cut < CutNames.Length && beard < BeardNames.Length;

        // True when the race exists and each option byte is one of its choices; a
        // slot the race doesn't use must be 0.
        public static bool Valid(byte race, byte a, byte b, byte c, byte d)
        {
            if (race >= Races.Length) return false;
            var options = Races[race].Options;
            byte[] values = { a, b, c, d };
            for (int i = 0; i < OptionSlots; i++)
                if (i < options.Length ? values[i] >= options[i].Count : values[i] != 0) return false;
            return true;
        }

        // A stable look for a player who has not chosen one, from their player id.
        public static (byte Skin, byte Hair, byte Cut, byte Beard) Default(uint playerId)
        {
            uint h = playerId * 2654435761u;
            h ^= h >> 15;
            h *= 2246822519u;
            h ^= h >> 13;
            return ((byte)(h % (uint)Skins), (byte)(h / 7 % (uint)Hairs), (byte)(h / 41 % (uint)CutNames.Length), (byte)(h / 211 % 3 == 0 ? 1 : 0));
        }
    }
}
