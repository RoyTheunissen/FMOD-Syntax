using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Utilities for loading assets.
    /// </summary>
    public static class ScriptableObjectUtilities
    {
        private static void EnsureFolderExists(string path)
        {
            string absolutePath = Path.Combine(Application.dataPath, path);

            if (Directory.Exists(absolutePath))
                return;

            Directory.CreateDirectory(absolutePath);
            AssetDatabase.Refresh();
        }
        
        public static Type CreateScriptableObject<Type>(string path, string name, bool selectAfterwards = false)
            where Type : ScriptableObject
        {
            ScriptableObject scriptableObject = ScriptableObject.CreateInstance(typeof(Type));
            scriptableObject.name = name;
            
            EnsureFolderExists(path);

            path = path.AddAssetsPrefix().AddSuffixIfMissing("/") + name.AddSuffixIfMissing(".asset");

            path = AssetDatabase.GenerateUniqueAssetPath(path);
            
            AssetDatabase.CreateAsset(scriptableObject, path);

            if (selectAfterwards)
                Selection.activeObject = scriptableObject;
            
            return (Type)scriptableObject;
        }

        public static Type CreateScriptableObjectAtCurrentFolder<Type>(string name, bool selectAfterwards = false)
            where Type : ScriptableObject
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return CreateScriptableObject<Type>(path, name, selectAfterwards);
        }

        public static Type CreateScriptableObjectAtCurrentFolder<Type>(bool selectAfterwards = false)
            where Type : ScriptableObject
        {
            return CreateScriptableObjectAtCurrentFolder<Type>(typeof(Type).Name, selectAfterwards);
        }
    }
}
