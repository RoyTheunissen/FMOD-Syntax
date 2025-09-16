using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public static class FloatExtensions
    {
        // These methods break my naming convention of methods always having to be verbs but their
        // use is unambiguous and the whole point of these methods is to make floating point based 
        // comparisons more compact and readable.
        public static bool Approximately(this float a, float b)
        {
            return Mathf.Approximately(a, b);
        }
    
        public static bool Approximately(this float a, float b, float tolerance)
        {
            return Mathf.Abs(a - b).EqualOrSmaller(tolerance);
        }

        public static bool Equal(this float a, float b)
        {
            return a.Approximately(b);
        }

        public static bool EqualOrGreater(this float a, float b)
        {
            return Equal(a, b) || a > b;
        }

        public static bool EqualOrSmaller(this float a, float b)
        {
            return Equal(a, b) || a < b;
        }

        public static bool Greater(this float a, float b)
        {
            return !Equal(a, b) && a > b;
        }

        public static bool Smaller(this float a, float b)
        {
            return !Equal(a, b) && a < b;
        }
        
        public static float Clamp(this float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }
        
        public static float Saturate(this float value)
        {
            return Clamp(value, 0, 1);
        }
    }
}
