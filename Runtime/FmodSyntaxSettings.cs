using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Scriptable object that holds all the settings for the FMOD Syntax system.
    /// </summary>
    public sealed class FmodSyntaxSettings : ScriptableObject 
    {
        public enum EventNameClashPreventionTypes
        {
            None = 0,
            GenerateSeparateClassesPerFolder = 1,
            IncludePath = 2,
        }
        
        [SerializeField] private string generatedScriptsFolderPath;
        public string GeneratedScriptsFolderPath => generatedScriptsFolderPath;

        [SerializeField] private string namespaceForGeneratedCode;
        public string NamespaceForGeneratedCode => namespaceForGeneratedCode;
        
        [SerializeField] private bool shouldGenerateAssemblyDefinition;
        public bool ShouldGenerateAssemblyDefinition => shouldGenerateAssemblyDefinition;
        
        [FormerlySerializedAs("generateFallbacksForMissingEvents")]
        [Tooltip("If specified, renamed or moved events will first generate an 'alias' so that any existing " +
                 "references so you can update the references without getting compile errors.")]
        [SerializeField] private bool generateFallbacksForChangedEvents = true;
        public bool GenerateFallbacksForChangedEvents => generateFallbacksForChangedEvents;

        [Header("Syntax Format")]
        [SerializeField] private EventNameClashPreventionTypes eventNameClashPreventionType
            = EventNameClashPreventionTypes.None;
        public EventNameClashPreventionTypes EventNameClashPreventionType => eventNameClashPreventionType;

        [NonSerialized] private static FmodSyntaxSettings cachedInstance;
        [NonSerialized] private static bool didCacheInstance;
        public static FmodSyntaxSettings Instance
        {
            get
            {
#if UNITY_EDITOR
                if (!didCacheInstance)
                {
                    string[] guids = AssetDatabase.FindAssets($"t:{nameof(FmodSyntaxSettings)}");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        cachedInstance = AssetDatabase.LoadAssetAtPath<FmodSyntaxSettings>(path);
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
