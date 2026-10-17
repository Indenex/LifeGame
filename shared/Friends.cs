using System;

// claims-32: friends (Justin picked the friends list, 2026-10-17). You mark people as friends;
// once they mark you too, each sees when the other is online and roughly where. One-way marks
// show nothing about the other person but their name, so nobody is tracked by someone they never
// chose. Numbers start low (D-075). Shared by the server and the client; no engine types here.
namespace LifeGame.Shared
{
    public static class Friends
    {
        // Friends one character can mark.
        public const int Max = 50;
        // How often the server notes where friends are, and how coarsely (metres): a friend's
        // spot is rounded, so the list says "40 m north", never an exact place.
        public const int SweepSeconds = 5;
        public const float SpotStep = 10f;

        // How far someone is and which way, as the group list says it (ui-78): "close by",
        // "40 m north", "far off to the southwest". Steps of 10 m and eight world compass words.
        // -Z is north and +X is east, as on the death marker.
        public static string HowFar(float dx, float dz)
        {
            float d = MathF.Sqrt(dx * dx + dz * dz);
            if (d < 12f) return "close by";
            double deg = Math.Atan2(dx, -dz) * 180.0 / Math.PI;
            int i = (int)(((Math.Round(deg / 45.0) % 8) + 8) % 8);
            string way = Compass[i];
            return d >= 300f ? $"far off to the {way}" : $"{(int)MathF.Round(d / 10f) * 10} m {way}";
        }

        static readonly string[] Compass = { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };
    }
}
