#if UNITY_AUDIO_SYNTAX && UNITY_AUDIO_SYNTAX_ADDRESSABLES

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Hooks into the Addressables build system to create a map from audio event paths to their address.
    /// This way if you know the path to an audio event, you can also figure out the address and load it individually.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityAudioEventAddressablesBuildHook
    {
        private const string AudioEventPathPropertyName = "audioEventPath";
        private const string AddressPropertyName = "address";
        
        private static SerializedProperty audioEventPathToAddressablePathsProperty;

        static UnityAudioEventAddressablesBuildHook()
        {
            BuildScript.buildCompleted -= OnBuildCompleted;
            BuildScript.buildCompleted += OnBuildCompleted;
        }

        static void OnBuildCompleted(AddressableAssetBuildResult result)
        {
            if (UnityAudioSyntaxSettings.Instance == null)
                return;

            SerializedObject serializedObject = new(UnityAudioSyntaxSettings.Instance); 
            serializedObject.Update();

            audioEventPathToAddressablePathsProperty =
                serializedObject.FindProperty("audioEventPathsToAddressablePaths");
            audioEventPathToAddressablePathsProperty.ClearArray();
            
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    ProcessEntry(entry);
                }
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Dispose();
        }

        private static void ProcessEntry(AddressableAssetEntry entry)
        {
            if (entry.MainAsset is UnityAudioEventConfigAssetBase config)
                ProcessConfig(config, entry);
            
            foreach (AddressableAssetEntry child in entry.SubAssets)
            {
                ProcessEntry(child);
            }
        }

        private static void ProcessConfig(UnityAudioEventConfigAssetBase config, AddressableAssetEntry entry)
        {
            // Map the audio event path to the address.
            SerializedProperty element = audioEventPathToAddressablePathsProperty.AddArrayElement();
            element.FindPropertyRelative(AudioEventPathPropertyName).stringValue = config.Path;
            element.FindPropertyRelative(AddressPropertyName).stringValue = entry.address;
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX && UNITY_AUDIO_SYNTAX_ADDRESSABLES
