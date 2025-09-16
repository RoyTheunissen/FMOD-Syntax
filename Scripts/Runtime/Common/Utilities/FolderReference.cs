namespace RoyTheunissen.AudioSyntax
{
    [System.Serializable]
    public class FolderReference
    {
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
