using System;
using System.Linq;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Useful extension methods for EditorParamRef.
    /// </summary>
    public static class TypeExtensions
    {
        public static Type[] GetAllTypesWithAttribute<T>(bool includeAbstract = true)
            where T : Attribute
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.HasAttribute<T>() && (includeAbstract || !t.IsAbstract)).ToArray();
        }
    }
}
