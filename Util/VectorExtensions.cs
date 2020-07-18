using OpenTK;
using System;

namespace BrewLib.Util
{
    public static class VectorExtensions
    {
        public static Vector2 Round(this Vector2 v) 
            => new Vector2((float)Math.Round(v.X), (float)Math.Round(v.Y));
        public static Vector3 Round(this Vector3 v) 
            => new Vector3((float)Math.Round(v.X), (float)Math.Round(v.Y), (float)Math.Round(v.Z));

        public static Vector2 ClampLength(this Vector2 v, float max)
        {
            var length = v.LengthSquared;
            return length > max * max ? v / (float)Math.Sqrt(length) : v;
        }
        public static Vector3 ClampLength(this Vector3 v, float max)
        {
            var length = v.LengthSquared;
            return length > max * max ? v / (float)Math.Sqrt(length) : v;
        }
    }
}
