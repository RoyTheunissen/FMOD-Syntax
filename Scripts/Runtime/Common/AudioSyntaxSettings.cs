using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Scriptable object that holds all the settings for the Audio Syntax system.
    /// </summary>
    public sealed class AudioSyntaxSettings : ScriptableObject
    {
        public enum SyntaxFormats
        {
            Flat = 0,
            FlatWithPathIncludedInName = 1,
            SubclassesPerFolder = 2,
        }
        
        public const int TargetVersion = 1;
        
        [SerializeField, HideInInspector] private int version;
        public int Version => version;

        [SerializeField] private AudioSyntaxSystems activeSystems;
        public AudioSyntaxSystems ActiveSystems => activeSystems;

        [SerializeField] private string generatedScriptsFolderPath = string.Empty;
        public string GeneratedScriptsFolderPath
        {
            get
            {
                string path = generatedScriptsFolderPath.ToUnityPath();
                
                // FIX: Make sure it ends with a / otherwise you can get some strange behaviour
                if (!path.EndsWith(Path.AltDirectorySeparatorChar))
                    path += Path.AltDirectorySeparatorChar;
                
                return path;
            }
        }

        [SerializeField] private string namespaceForGeneratedCode = string.Empty;
        public string NamespaceForGeneratedCode => namespaceForGeneratedCode;
        
        [SerializeField] private bool shouldGenerateAssemblyDefinition;
        public bool ShouldGenerateAssemblyDefinition => shouldGenerateAssemblyDefinition;
        
        [FormerlySerializedAs("generateFallbacksForMissingEvents")]
        [Tooltip("If specified, renamed or moved events will first generate an 'alias' so that any existing " +
                 "references so you can update the references without getting compile errors.")]
        [SerializeField] private bool generateFallbacksForChangedEvents = true;
        public bool GenerateFallbacksForChangedEvents => generateFallbacksForChangedEvents;
        
        [Space]
        [Tooltip("How to format the events syntax. Different formats suit different use cases:\n\n" +
                 "Flat - Simplest syntax. All events are inside a class called AudioEvents. Event names have to be unique.\n\n" +
                 "Flat (With Path Included In Name) - Like Flat but an event called 'Player/Footstep' would generate " +
                 "a field called 'Player_Footstep'. Keeps things very simple but does prevent name conflicts.\n\n" +
                 "Subclasses Per Folder - Generates subclasses inside AudioEvents to represent the " +
                 "folders in the FMOD project. An event called 'Player/Footstep' would be accessed via " +
                 "'AudioEvents.Player.Footstep'. A very clear and organized way to prevent name clashes but does " +
                 "require more typing.")]
        [SerializeField] private SyntaxFormats syntaxFormat = SyntaxFormats.Flat;
        public SyntaxFormats SyntaxFormat => syntaxFormat;
        
        [NonSerialized] private static AudioSyntaxSettings cachedInstance;
        [NonSerialized] private static bool didCacheInstance;
        public static AudioSyntaxSettings Instance
        {
            get
            {
#if UNITY_EDITOR
                if (!didCacheInstance || cachedInstance == null)
                {
                    string[] guids = AssetDatabase.FindAssets($"t:{nameof(AudioSyntaxSettings)}");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        cachedInstance = AssetDatabase.LoadAssetAtPath<AudioSyntaxSettings>(path);
                        didCacheInstance = cachedInstance != null;
                    }
                }
#endif // UNITY_EDITOR
                return cachedInstance;
            }
        }

        public void InitializeFromWizard(AudioSyntaxSystems activeSystems,
            string generatedScriptsFolderPath, string namespaceForGeneratedCode, bool shouldGenerateAssemblyDefinition)
        {
            this.activeSystems = activeSystems;
            
            // Sanitize the generated scripts folder path.
            this.generatedScriptsFolderPath = generatedScriptsFolderPath.Replace(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!this.generatedScriptsFolderPath.EndsWith(Path.AltDirectorySeparatorChar))
                this.generatedScriptsFolderPath += Path.AltDirectorySeparatorChar;
            
            this.namespaceForGeneratedCode = namespaceForGeneratedCode;

            this.shouldGenerateAssemblyDefinition = shouldGenerateAssemblyDefinition;

            version = TargetVersion;
        }
        
#if UNITY_EDITOR
        private const int OpenSettingsPriority = 100;
        private const string OpenSettingsMenuPath = AudioSyntaxMenuPaths.Root + "Open General Settings File";
        
        [MenuItem(OpenSettingsMenuPath, false, OpenSettingsPriority)]
        public static void OpenSettings()
        {
            Selection.activeObject = Instance;
            EditorGUIUtility.PingObject(Instance);
        }
        
        [MenuItem(OpenSettingsMenuPath, true, OpenSettingsPriority)]
        public static bool OpenSettingsValidation()
        {
            return Instance != null;
        }
#endif // UNITY_EDITOR
    }
}
