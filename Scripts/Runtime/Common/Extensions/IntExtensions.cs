using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public static class IntExtensions
    {
        public static int Modulo(this int x, int divisor)
        {
            return (x % divisor + divisor) % divisor;
        }
    
        public static bool IsEven(this int x)
        {
            return x.Modulo(2) == 0;
        }
    
        public static int Abs(this int value)
        {
            return Mathf.Abs(value);
        }
    
        public static int Clamp(this int x, int min, int max)
        {
            return Mathf.Clamp(x, min, max);
        }
    
        public static int GetSign(this int value)
        {
            if (value == 0)
                return 0;
            return value > 0 ? 1 : -1;
        }

        public static int SetSign(this int value, int sign, bool ignoreZero = false)
        {
            if (ignoreZero && sign == 0)
                return value;
        
            return value.Abs() * sign.GetSign();
        }
    
        public static int Min(this int value, params int[] values)
        {
            return Mathf.Min(value, Mathf.Min(values));
        }
    
        public static int Max(this int value, params int[] values)
        {
            return Mathf.Max(value, Mathf.Max(values));
        }
    }
}
