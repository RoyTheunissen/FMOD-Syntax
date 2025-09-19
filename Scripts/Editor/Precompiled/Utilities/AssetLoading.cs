using System;
using UnityEditor;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Utilities for loading assets.
    /// </summary>
    public static class AssetLoading
    {
        private static string[] GetGuidsOfAllAssetsOfType(Type type, string[] searchInFolders = null)
        {
            return AssetDatabase.FindAssets("t:" + type.Name, searchInFolders);
        }
        
        private static string[] GetGuidsOfAllAssetsOfType<T>(string[] searchInFolders = null) where T : UnityEngine.Object
        {
            return GetGuidsOfAllAssetsOfType(typeof(T), searchInFolders);
        }

        public static T[] GetAllAssetsOfType<T>(string[] searchInFolders = null) where T : UnityEngine.Object
        {
            string[] guids = GetGuidsOfAllAssetsOfType<T>(searchInFolders);

            T[] result = new T[guids.Length];

            for (int i = 0; i < guids.Length; ++i)
            {
                result[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
            return result;
        }
    }
}
