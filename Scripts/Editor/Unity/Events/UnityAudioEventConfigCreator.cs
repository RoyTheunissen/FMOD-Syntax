#if UNITY_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public class UnityAudioEventConfigCreator : Editor
    {
        private const string ConfigExtension = ".asset";

        private const string BasePath = "Assets/Create/" + AudioSyntaxMenuPaths.CreateUnityAudioConfig;
        
        private const string CreateConfigsFromClipsMenuText = BasePath + "Events From Selected Clips";

        private const string CreateOneOffEventsMenuText = BasePath + "Event (One-Off)";
        private const string CreateLoopingEventsMenuText = BasePath + "Event (Looping)";
        
        private const int Priority = AudioSyntaxMenuPaths.CreateMenuPriority + 100;

        private static Dictionary<string, List<AudioClip>> GetAudioClipsByNameRoot()
        {
            Dictionary<string, List<AudioClip>> audioClipsByNameRoot = new();
            
            // Find all selected audio clips and group them by the root of their filename (without any number suffix).
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                if (!(Selection.objects[i] is AudioClip audioClip))
                    continue;
                
                string pathClip = AssetDatabase.GetAssetPath(audioClip);
                string directory = Path.GetDirectoryName(pathClip);
                string fileName = Path.GetFileNameWithoutExtension(pathClip);
                
                fileName.GetNumberSuffix(out string fileNameRoot, out string _);

                string pathConfig = Path.Combine(directory, fileNameRoot);

                bool existed = audioClipsByNameRoot.TryGetValue(pathConfig, out List<AudioClip> audioClips);
                if (!existed)
                {
                    audioClips = new List<AudioClip>();
                    audioClipsByNameRoot.Add(pathConfig, audioClips);
                }
                
                audioClips.Add(audioClip);
            }

            return audioClipsByNameRoot;
        }
        
        [MenuItem(CreateOneOffEventsMenuText, false, AudioSyntaxMenuPaths.CreateMenuPriority)]
        public static void CreateOneOffAudioEventConfigsFromSelection()
        {
            Dictionary<string, List<AudioClip>> audioClipsByNameRoot = GetAudioClipsByNameRoot();

            if (audioClipsByNameRoot.Count == 0)
            {
                ScriptableObjectUtilities.CreateScriptableObjectAtCurrentFolder<UnityAudioEventOneOffConfigAsset>(
                    "One-Off");
                return;
            }
            
            foreach (KeyValuePair<string, List<AudioClip>> kvp in audioClipsByNameRoot)
            {
                CreateAudioOneOffConfig(kvp.Key, kvp.Value);
            }
        }
        
        [MenuItem(CreateLoopingEventsMenuText, false, AudioSyntaxMenuPaths.CreateMenuPriority + 1)]
        public static void CreateLoopingAudioEventConfigsFromSelection()
        {
            Dictionary<string, List<AudioClip>> audioClipsByNameRoot = GetAudioClipsByNameRoot();
            
            if (audioClipsByNameRoot.Count == 0)
            {
                ScriptableObjectUtilities.CreateScriptableObjectAtCurrentFolder<UnityAudioEventLoopingConfigAsset>(
                    "Looping");
                return;
            }

            foreach (KeyValuePair<string, List<AudioClip>> kvp in audioClipsByNameRoot)
            {
                CreateAudioLoopingConfig(kvp.Key, kvp.Value);
            }
        }

        private static string GetConfigPath(string audioClipPath)
        {
            audioClipPath = audioClipPath.ToUnityPath();
            
            string fileName = Path.GetFileName(audioClipPath);
            
            // Separate the config path into directory and filename.
            string directoryOriginal = audioClipPath.Substring(0, audioClipPath.Length - fileName.Length);
            
            // Clean up the filename
            fileName = Path.GetFileNameWithoutExtension(fileName).ToHumanReadable().TrimEnd();
            
            UnityAudioSyntaxSettings settings = UnityAudioSyntaxSettings.Instance;
            
            // A good fallback is to just put it in the root of the event configs folder
            // and let the user move it to an appropriate subfolder.
            string directoryFinal = settings.AudioEventConfigAssetRootFolder
                .ToUnityPath().AddSuffixIfMissing("/").AddAssetsPrefix();
            if (settings.AudioClipFoldersMirrorEventFolders)
            {
                string prefixToRemove = UnityAudioSyntaxSettings.Instance.AudioClipRootFolder
                    .ToUnityPath().AddSuffixIfMissing("/").AddAssetsPrefix();;

                if (directoryOriginal.StartsWith(prefixToRemove))
                {
                    string directoryRelativeToAudioClipRoot = directoryOriginal.RemovePrefix(prefixToRemove);
                    directoryFinal = Path.Combine(
                        settings.AudioEventConfigAssetRootFolder, directoryRelativeToAudioClipRoot).ToUnityPath();
                }
            }

            // Recombine them to form the final config path.
            string configPath = (Path.Combine(directoryFinal, fileName) + ConfigExtension)
                .ToUnityPath().AddAssetsPrefix();
            return configPath;
        }

        private static void CreateAudioConfig<ConfigType>(string pathRoot, Action<ConfigType, SerializedObject> callback)
            where ConfigType : UnityAudioEventConfigAssetBase
        {
            string pathConfig = GetConfigPath(pathRoot);

            ConfigType config = AssetDatabase.LoadAssetAtPath<ConfigType>(pathConfig);
            bool didExist = config != null;

            if (!didExist)
            {
                // Make sure the directory exists.
                Directory.CreateDirectory(Path.GetDirectoryName(pathConfig));
            
                // Create an asset for the new config. 
                config = ScriptableObject.CreateInstance<ConfigType>();
                AssetDatabase.CreateAsset(config, pathConfig);
            }

            // Allow the config to be initialized with the correct data.
            SerializedObject serializedObject = new SerializedObject(config);
            serializedObject.Update();
            callback(config, serializedObject);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static void AddAudioClipToAudioClipsProperty(SerializedProperty property, AudioClip audioClip)
        {
            // The property is a wrapper object of type 'AudioEventConfigPropertyAudioClips'.
            // This is to be able to tie functionality to Parameters.
            // This also means that the *actual* list of Audio Clips is one level deeper.
            SerializedProperty valueProperty = property.FindPropertyRelative("value");

            const string audioClipPropertyName = "audioClip";
            
            // First do a quick check to see if the audio clip in question is already present.
            // If so, there is no use in adding it again.
            bool alreadyContained = false;
            for (int i = 0; i < valueProperty.arraySize; i++)
            {
                SerializedProperty existingAudioClipMetadata = valueProperty.GetArrayElementAtIndex(i);
                SerializedProperty existingAudioClipProperty =
                    existingAudioClipMetadata.FindPropertyRelative(audioClipPropertyName);
                if (existingAudioClipProperty.objectReferenceValue == audioClip)
                {
                    alreadyContained = true;
                    break;
                }
            }
            if (alreadyContained)
                return;
                    
            // The list does not directly reference Audio Clips either. They are of type AudioClipMetaData,
            // for similar reasons; we want to allow users to specify extra data related to this audio clip,
            // such as being able to specify Timeline Events.
            // This also means that the actual audio clip property is one level deeper.
            SerializedProperty audioClipMetaDataProperty = valueProperty.AddArrayElement();
            SerializedProperty audioClipProperty =
                audioClipMetaDataProperty.FindPropertyRelative(audioClipPropertyName);
            
            audioClipProperty.objectReferenceValue = audioClip;
        }

        private static void CreateAudioOneOffConfig(string pathRoot, List<AudioClip> audioClipsToAdd)
        {
            CreateAudioConfig<UnityAudioEventOneOffConfigAsset>(pathRoot,
                (config, serializedObject) =>
                {
                    SerializedProperty audioClipsPropertyProperty = serializedObject.FindProperty("audioClips");
                    
                    // Now make sure the selected clips are added if they weren't already there.
                    foreach (AudioClip audioClipToAdd in audioClipsToAdd)
                    {
                        AddAudioClipToAudioClipsProperty(audioClipsPropertyProperty, audioClipToAdd);
                    }
                });
        }
        
        private static void CreateAudioLoopingConfig(string pathRoot, List<AudioClip> audioClipsToAdd)
        {
            CreateAudioConfig<UnityAudioEventLoopingConfigAsset>(pathRoot,
                (config, serializedObject) =>
                {
                    SerializedProperty loopingAudioClipPropertyProperty =
                        serializedObject.FindProperty("loopingAudioClips");
                    foreach (AudioClip audioClipToAdd in audioClipsToAdd)
                    {
                        AddAudioClipToAudioClipsProperty(loopingAudioClipPropertyProperty, audioClipToAdd);
                    }
                });
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX
