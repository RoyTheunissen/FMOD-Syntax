using System;
using System.Collections.Generic;
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
        private static readonly string SettingsFilename = nameof(UnityAudioSyntaxSettings);
        public static readonly string SettingsFilenameIncludingExtension = SettingsFilename + ".asset";
        public static readonly string PathRelativeToResources = $"AudioSyntax/";
        
        // This is used at runtime, do not remove
        public static readonly string SettingsPathRelativeToResources = PathRelativeToResources + SettingsFilename;
        
        [SerializeField] private AudioSource audioSourcePooledPrefab;
        public AudioSource AudioSourcePooledPrefab => audioSourcePooledPrefab;

        [SerializeField] private AudioMixerGroup defaultMixerGroup;
        public AudioMixerGroup DefaultMixerGroup => defaultMixerGroup;
        
        [Tooltip("Use this to specify the root folder in which all your Unity Audio Event Configs are located. " +
                 "This is used at editor time to figure out short and useful relative paths for events based on the " +
                 "folder structure. At runtime, this is used to figure out where a config is relative to the " +
                 "Resources folder so it can be loaded.")]
        [SerializeField, HideInInspector] private string audioEventConfigAssetRootFolder;
        public string AudioEventConfigAssetRootFolder => audioEventConfigAssetRootFolder;
        
        [SerializeField, HideInInspector] private AudioEventPathToAddress[] audioEventPathsToAddressablePaths;
        
        [NonSerialized] private string cachedUnityAudioEventConfigAssetRootFolderRelativeToResources;
        [NonSerialized] private bool didCacheUnityAudioEventConfigAssetRootFolderRelativeToResources;
        public string UnityAudioEventConfigAssetRootFolderRelativeToResources
        {
            get
            {
                if (!didCacheUnityAudioEventConfigAssetRootFolderRelativeToResources || !Application.isPlaying)
                {
                    didCacheUnityAudioEventConfigAssetRootFolderRelativeToResources = true;
                    
                    bool didFindValidPath = FindPathRelativeToResources(
                        AudioEventConfigAssetRootFolder, out string path);
                    if (!didFindValidPath)
                    {
                        Debug.LogError($"Tried to determine Unity Audio Event Config Asset Root path relative to the " +
                                       $"Resources folder, but it seems like the specified path '{path}' is not " +
                                       $"relative to the Resources folder at all. Please review your " +
                                       $"Unity Audio Syntax Settings file.", this);
                    }

                    cachedUnityAudioEventConfigAssetRootFolderRelativeToResources = path;
                }
                return cachedUnityAudioEventConfigAssetRootFolderRelativeToResources;
            }
        }
        
        [NonSerialized] private readonly Dictionary<string, string> cachedAudioEventPathsToAddressablePathsDictionary
            = new();
        [NonSerialized] private bool didCacheAudioEventPathsToAddressablePathsDictionary;
        private Dictionary<string, string> AudioEventPathsToAddressablePathsDictionary
        {
            get
            {
                if (!didCacheAudioEventPathsToAddressablePathsDictionary)
                {
                    didCacheAudioEventPathsToAddressablePathsDictionary = true;

                    // Format the list as a dictionary for performance.
                    for (int i = 0; i < audioEventPathsToAddressablePaths.Length; i++)
                    {
                        AudioEventPathToAddress audioEventPathToAddress = audioEventPathsToAddressablePaths[i];
                        cachedAudioEventPathsToAddressablePathsDictionary.Add(
                            audioEventPathToAddress.AudioEventPath, audioEventPathToAddress.Address);
                    }
                }
                return cachedAudioEventPathsToAddressablePathsDictionary;
            }
        }

        [NonSerialized] private static UnityAudioSyntaxSettings cachedInstance;
        [NonSerialized] private static bool didCacheInstance;
        public static UnityAudioSyntaxSettings Instance
        {
            get
            {
                if (!didCacheInstance || (Application.isEditor && cachedInstance == null))
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
                    cachedInstance = Resources.Load<UnityAudioSyntaxSettings>(SettingsPathRelativeToResources);
#endif // UNITY_EDITOR
                }
                return cachedInstance;
            }
        }

        public void InitializeFromWizard(
            AudioSource audioSourcePooledPrefab, AudioMixerGroup defaultMixerGroup,
            string audioEventConfigAssetRootFolder)
        {
            this.audioSourcePooledPrefab = audioSourcePooledPrefab;
            this.defaultMixerGroup = defaultMixerGroup;
            this.audioEventConfigAssetRootFolder = audioEventConfigAssetRootFolder;
        }

        private static bool FindPathRelativeToResources(string path, out string pathRelativeToResources)
        {
            bool isValid = true;
            
            const string resources = "Resources";
            path = path.ToUnityPath().RemoveSuffix("/");
            
            if (string.IsNullOrEmpty(path) || string.Equals(path, resources, StringComparison.OrdinalIgnoreCase)
                                           || path.EndsWith("/" + resources))
            {
                // Ends with Resources/
                path = string.Empty;
            }
            else if (path.StartsWith(resources + "/"))
            {
                // Starts with Resources/
                path = path.Substring(resources.Length + 1);
            }
            else
            {
                // Check if it has /Resources/ somewhere in the middle
                const string resourcesWithSlashes = "/" + resources + "/";
                int resourcesFolderIndex = path.IndexOf(resourcesWithSlashes, StringComparison.OrdinalIgnoreCase);
                
                if (resourcesFolderIndex == -1)
                {
                    // Didn't have Resources in it at all. That means the path is invalid.
                    path = string.Empty;
                    isValid = false;
                }
                else
                {
                    path = path.Substring(resourcesFolderIndex + resourcesWithSlashes.Length);
                }
            }

            if (!string.IsNullOrEmpty(path) && !path.EndsWith("/"))
                path += "/";

            pathRelativeToResources = path;
            
            return isValid;
        }

        public bool GetAddressForAudioEventPath(string audioEventPath, out string address)
        {
            return AudioEventPathsToAddressablePathsDictionary.TryGetValue(audioEventPath, out address);
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
        
        public static string GetFilteredPathForUnityAudioEventConfig(UnityAudioEventConfigAssetBase config)
        {
            string assetPath = AssetDatabase.GetAssetPath(config);
            string filteredPath = assetPath.RemoveAssetsPrefix().RemoveSuffix(".asset");

            string basePath = Instance == null
                ? string.Empty
                : Instance.AudioEventConfigAssetRootFolder.ToUnityPath();
            
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
