using OpenTK;
using System;

namespace BrewLib.Util
{
    public static class VectorHelper
    {
        public static Vector2 FromPolar(float angle, float length = 1) => new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * length;
    }
}
