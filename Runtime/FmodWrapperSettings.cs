using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace RoyTheunissen.FMODWrapper.Runtime
{
    /// <summary>
    /// Scriptable object that holds all the settings for the FMOD wrapper system.
    /// </summary>
    public sealed class FmodWrapperSettings : ScriptableObject 
    {
        [SerializeField] private string generatedScriptsFolderPath;
        public string GeneratedScriptsFolderPath => generatedScriptsFolderPath;

        [SerializeField] private string namespaceForGeneratedCode;
        public string NamespaceForGeneratedCode => namespaceForGeneratedCode;
        
        [SerializeField] private bool shouldGenerateAssemblyDefinition;
        public bool ShouldGenerateAssemblyDefinition => shouldGenerateAssemblyDefinition;

        [NonSerialized] private static FmodWrapperSettings cachedInstance;
        [NonSerialized] private static bool didCacheInstance;
        public static FmodWrapperSettings Instance
        {
            get
            {
#if UNITY_EDITOR
                if (!didCacheInstance)
                {
                    string[] guids = AssetDatabase.FindAssets($"t:{nameof(FmodWrapperSettings)}");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        cachedInstance = AssetDatabase.LoadAssetAtPath<FmodWrapperSettings>(path);
                        didCacheInstance = cachedInstance != null;
                    }
                }
#endif // UNITY_EDITOR
                return cachedInstance;
            }
        }

        public void InitializeFromWizard(
            string generatedScriptsFolderPath, string namespaceForGeneratedCode, bool shouldGenerateAssemblyDefinition)
        {
            // Sanitize the generated scripts folder path.
            this.generatedScriptsFolderPath = generatedScriptsFolderPath.Replace(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!this.generatedScriptsFolderPath.EndsWith(Path.AltDirectorySeparatorChar))
                this.generatedScriptsFolderPath += Path.AltDirectorySeparatorChar;
            
            this.namespaceForGeneratedCode = namespaceForGeneratedCode;

            this.shouldGenerateAssemblyDefinition = shouldGenerateAssemblyDefinition;
        }
    }
}
