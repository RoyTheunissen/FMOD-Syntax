using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    [System.Serializable]
    public class FolderReference
    {
        [SerializeField] private string name;

        public string GUID;

#if UNITY_EDITOR
        public string Path
        {
            get => UnityEditor.AssetDatabase.GUIDToAssetPath(GUID);
            set => GUID = UnityEditor.AssetDatabase.AssetPathToGUID(value);
        }
#endif // UNITY_EDITOR
        
        public FolderReference(string path) => Path = path;
    }
}
