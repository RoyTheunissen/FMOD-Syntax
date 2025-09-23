using System;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Scriptable object that holds all the settings for the Unity Audio Syntax system.
    /// </summary>
    public sealed class UnityAudioSyntaxSettings : ScriptableObject 
    {
        public static readonly string SettingsFilename = $"{nameof(UnityAudioSyntaxSettings)}.asset";
        public static readonly string PathRelativeToResources = $"AudioSyntax/" + SettingsFilename;
        
        [SerializeField] private AudioSource audioSourcePooledPrefab;
        public AudioSource AudioSourcePooledPrefab => audioSourcePooledPrefab;

        [SerializeField] private AudioMixerGroup defaultMixerGroup;
        public AudioMixerGroup DefaultMixerGroup => defaultMixerGroup;
        
        [Tooltip("Use this to specify the root folder in which all your Unity Audio Configs are located. " +
                 "If you don't do this, it will try and infer what the location is using well-known Unity project " +
                 "structures, but this is not guaranteed to work.")]
        [SerializeField] private FolderReference unityAudioConfigRootFolder;
        public FolderReference UnityAudioConfigRootFolder => unityAudioConfigRootFolder;

        [NonSerialized] private static UnityAudioSyntaxSettings cachedInstance;
        [NonSerialized] private static bool didCacheInstance;
        public static UnityAudioSyntaxSettings Instance
        {
            get
            {
                if (!didCacheInstance)
                {
#if UNITY_EDITOR
                    string[] guids = AssetDatabase.FindAssets($"t:{nameof(UnityAudioSyntaxSettings)}");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        cachedInstance = AssetDatabase.LoadAssetAtPath<UnityAudioSyntaxSettings>(path);
                        didCacheInstance = cachedInstance != null;
                    }
#else
                    cachedInstance = Resources.Load<UnityAudioSyntaxSettings>(PathRelativeToResources);
#endif // UNITY_EDITOR
                }
                return cachedInstance;
            }
        }

        public void InitializeFromWizard(
            AudioSource audioSourcePooledPrefab, AudioMixerGroup defaultMixerGroup, string unityAudioConfigRootFolder)
        {
            this.audioSourcePooledPrefab = audioSourcePooledPrefab;
            this.defaultMixerGroup = defaultMixerGroup;
            this.unityAudioConfigRootFolder = new FolderReference(unityAudioConfigRootFolder);
        }
        
#if UNITY_EDITOR && UNITY_AUDIO_SYNTAX
        private const string OpenSettingsMenuPath = AudioSyntaxMenuPaths.Root + "Open Unity Settings File";
        
        [MenuItem(OpenSettingsMenuPath, false)]
        public static void OpenSettings()
        {
            Selection.activeObject = Instance;
            EditorGUIUtility.PingObject(Instance);
        }
        
        [MenuItem(OpenSettingsMenuPath, true)]
        public static bool OpenSettingsValidation()
        {
            return Instance != null;
        }
        
        public static string GetFilteredPathForUnityAudioEventConfig(UnityAudioEventConfigBase config)
        {
            string assetPath = AssetDatabase.GetAssetPath(config);
            string filteredPath = assetPath.RemoveSuffix(".asset");

            string basePath = Instance == null
                ? string.Empty
                : Instance.UnityAudioConfigRootFolder.Path.ToUnityPath();
            
            if (!string.IsNullOrEmpty(basePath))
            {
                if (!basePath.EndsWith("/"))
                    basePath += "/";
                
                // Determine the path relative to the specified folder.
                filteredPath = filteredPath.RemovePrefix(basePath);
            }
            else
            {
                // No base folder was defined. Let's TRY and have some intelligent filtering.

                // No use in specifying that it's in the Assets folder. We know. 
                filteredPath = filteredPath.RemoveAssetsPrefix();
                
                // This used to do the same base path inference that the setup wizard now does, but that seems overkill
                // to do this every time you ask for a path. If you want shorter paths, then set up the base path
                // correctly. That is much more performant.
            }
            
            return filteredPath;
        }
#endif // UNITY_EDITOR
    }
}
