using System;

namespace RoyTheunissen.AudioSyntax
{
    public static class ArrayExtensions
    {
        public static T[] RemoveValue<T>(this T[] array, T valueToRemove)
            where T : IEquatable<T>
        {
            int index = Array.IndexOf(array, valueToRemove);
            if (index < 0) 
                return array;

            T[] result = new T[array.Length - 1];
    
            // Copy before the removed element
            if (index > 0)
                Array.Copy(array, 0, result, 0, index);
    
            // Copy after the removed element
            if (index < array.Length - 1)
                Array.Copy(array, index + 1, result, index, array.Length - index - 1);

            return result;
        }
    }
}
