using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoyTheunissen.FMODSyntax
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
                    cachedAsset = Resources.Load<T>(path);
                return cachedAsset;
            }
        }

        [NonSerialized] private readonly string path;
        public string Path => path;

        public EditorAssetReference(string path)
        {
            this.path = path;
        }

        public static implicit operator T(EditorAssetReference<T> editorAssetReference)
        {
            return editorAssetReference.Asset;
        }
    }
}
