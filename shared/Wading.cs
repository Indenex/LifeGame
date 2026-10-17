namespace LifeGame.Shared
{
    // Walking in water (bugfix-32). Water is one flat plane at World.WaterLevel and nobody is
    // walled out of it. Feet follow the bed while it is shallow, which slows the walk a little
    // more with every centimetre of depth. Where the bed is deeper than FloatDepth the body
    // floats with its feet at WaterLevel - FloatDepth (head and shoulders stay out) and moves at
    // about half speed. D-075: gentle numbers; no stamina cost, no new animation.
    public static class Wading
    {
        // Deeper than this and the feet stop following the bed.
        public const float FloatDepth = 0.9f;
        // Walking speed left at FloatDepth, and once properly afloat, as fractions of full speed.
        public const float WadeFloor = 0.6f;
        public const float FloatSpeed = 0.5f;
        // The slow-down from WadeFloor to FloatSpeed spreads over this much more depth.
        public const float FloatEase = 0.5f;

        // How far under the surface the bed lies at a point (0 on dry land).
        public static float Depth(float bedHeight) => System.Math.Max(0f, World.WaterLevel - bedHeight);

        public static bool Floating(float depth) => depth > FloatDepth;

        // Where the feet are: on the bed, or at the float line over deep water.
        public static float FeetY(float bedHeight) => System.Math.Max(bedHeight, World.WaterLevel - FloatDepth);

        // Fraction of walking speed at a depth. Eased, so there is no step at the shore.
        public static float SpeedFactor(float depth)
        {
            if (depth <= 0f) return 1f;
            float t = System.Math.Min(depth / FloatDepth, 1f);
            float eased = t * t * (3f - 2f * t);
            float f = 1f + (WadeFloor - 1f) * eased;
            if (depth <= FloatDepth) return f;
            float u = System.Math.Min((depth - FloatDepth) / FloatEase, 1f);
            return WadeFloor + (FloatSpeed - WadeFloor) * (u * u * (3f - 2f * u));
        }
    }
}
