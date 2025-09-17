using System;
using UnityEditor;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Utilities for loading assets.
    /// </summary>
    public static class AssetLoading
    {
        private static string[] GetGuidsOfAllAssetsOfType(Type type)
        {
            return AssetDatabase.FindAssets("t:" + type.Name);
        }
        
        private static string[] GetGuidsOfAllAssetsOfType<T>() where T : UnityEngine.Object
        {
            return GetGuidsOfAllAssetsOfType(typeof(T));
        }

        public static T[] GetAllAssetsOfType<T>() where T : UnityEngine.Object
        {
            string[] guids = GetGuidsOfAllAssetsOfType<T>();

            T[] result = new T[guids.Length];

            for (int i = 0; i < guids.Length; ++i)
            {
                result[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
            return result;
        }
    }
}
