using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace RoyTheunissen.FMODWrapper
{
    /// <summary>
    /// Helps you access an asset at editor time. Useful for finding code templates, for example.
    /// </summary>
    public class EditorAssetReference<T> where T : Object
    {
        [NonSerialized] private T cachedAsset;
        public T Asset
        {
            get
            {
                if (cachedAsset == null)
                {
                    string[] candidates = AssetDatabase.FindAssets($"t:{typeof(T).Name} {searchQuery}");
                    if (candidates.Length > 0)
                    {
                        string codeTemplatePath = AssetDatabase.GUIDToAssetPath(candidates[0]);
                        cachedAsset = AssetDatabase.LoadAssetAtPath<T>(codeTemplatePath);
                    }
                }
                return cachedAsset;
            }
        }

        [NonSerialized] private string searchQuery;

        public EditorAssetReference(string searchQuery)
        {
            this.searchQuery = searchQuery;
        }

        public static implicit operator T(EditorAssetReference<T> editorAssetReference)
        {
            return editorAssetReference.Asset;
        }
    }
}
