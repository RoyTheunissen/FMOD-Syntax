using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public static class MonoScriptExtensions
    {
        public static void SetText(this MonoScript monoScript, string text)
        {
            string projectRelativePath = AssetDatabase.GetAssetPath(monoScript);
            string absolutePath = projectRelativePath.GetAbsolutePath();

            if (!File.Exists(absolutePath))
            {
                Debug.LogError($"Tried to set text on MonoScript at '{absolutePath}' but that file did not seem to exist.");
                return;
            }
            
            File.WriteAllText(absolutePath, text);
        }
    }
}
