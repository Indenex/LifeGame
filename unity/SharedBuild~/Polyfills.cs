// netstandard2.1 lacks the marker type C# 9+ records and init setters need.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace LifeGame.Shared
{
    // netstandard2.1's System.MathF has no Tau. Inside LifeGame.Shared this type wins
    // over System.MathF, so shared/ compiles unchanged; every other member forwards.
    internal static class MathF
    {
        public const float PI = System.MathF.PI;
        public const float Tau = 2f * System.MathF.PI;
        public static float Abs(float x) => System.MathF.Abs(x);
        public static float Atan(float x) => System.MathF.Atan(x);
        public static float Atan2(float y, float x) => System.MathF.Atan2(y, x);
        public static float Ceiling(float x) => System.MathF.Ceiling(x);
        public static float Cos(float x) => System.MathF.Cos(x);
        public static float Sin(float x) => System.MathF.Sin(x);
        public static float Floor(float x) => System.MathF.Floor(x);
        public static float Log(float x) => System.MathF.Log(x);
        public static float Log(float x, float b) => System.MathF.Log(x, b);
        public static float Max(float a, float b) => System.MathF.Max(a, b);
        public static float Min(float a, float b) => System.MathF.Min(a, b);
        public static float Pow(float x, float y) => System.MathF.Pow(x, y);
        public static float Round(float x) => System.MathF.Round(x);
        public static float Round(float x, int digits) => System.MathF.Round(x, digits);
        public static float Round(float x, System.MidpointRounding mode) => System.MathF.Round(x, mode);
        public static float Sqrt(float x) => System.MathF.Sqrt(x);
    }
}
