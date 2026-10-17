// What a player can look like: skin tone, hair colour, haircut and beard, chosen
// by the player and stored on the server (model-3, D-233). Shared by the server
// module and the client so both agree on what is allowed. Keep this file free of
// engine and SpacetimeDB types.
namespace LifeGame.Shared
{
    public static class Looks
    {
        // How many skin tones and hair colours there are. The colours themselves
        // live in the client (HumanFigure).
        public const int Skins = 5, Hairs = 5;

        // Names for the picker, in the order of the cut and beard numbers.
        public static readonly string[] CutNames = { "Locks", "Cropped", "Long", "Tied back" };
        public static readonly string[] BeardNames = { "None", "Short" };

        // True when every choice is one the game has.
        public static bool Valid(byte skin, byte hair, byte cut, byte beard) =>
            skin < Skins && hair < Hairs && cut < CutNames.Length && beard < BeardNames.Length;

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
