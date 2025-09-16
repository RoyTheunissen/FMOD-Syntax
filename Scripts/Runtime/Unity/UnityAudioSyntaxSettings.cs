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
    [CreateAssetMenu(
        menuName = AudioSyntaxMenuPaths.CreateScriptableObject + "Unity Audio Syntax Settings",
        fileName = nameof(UnityAudioSyntaxSettings))]
    public sealed class UnityAudioSyntaxSettings : ScriptableObject 
    {
        public static readonly string PathSuffix = $"AudioSyntax/{nameof(UnityAudioSyntaxSettings)}.asset";
        
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
#if !UNITY_EDITOR
                    string[] guids = AssetDatabase.FindAssets($"t:{nameof(UnityAudioSyntaxSettings)}");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        cachedInstance = AssetDatabase.LoadAssetAtPath<UnityAudioSyntaxSettings>(path);
                        didCacheInstance = cachedInstance != null;
                    }
#else
                    cachedInstance = Resources.Load<UnityAudioSyntaxSettings>(PathSuffix);
#endif // UNITY_EDITOR
                }
                return cachedInstance;
            }
        }
    }
}
