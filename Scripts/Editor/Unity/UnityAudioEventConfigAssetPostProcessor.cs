using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public class UnityAudioEventConfigAssetPostProcessor : AssetPostprocessor
    {
        /// <summary>
        /// Asset Post Processor to check if a Unity Audio Event Config Asset was moved and do some additional checks,
        /// such as making sure it's a child of the specified root folder and updating the path field inside the asset.
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            const string extension = ".asset";

            for (int i = 0; i < movedAssets.Length; i++)
            {
                if (!movedAssets[i].EndsWith(extension))
                    continue;

                // If the moved asset was a Unity Audio Event Config Asset, then we need to make sure that its path
                // is correct, otherwise it will be out of date.
                UnityAudioEventConfigAssetBase config =
                    AssetDatabase.LoadAssetAtPath<UnityAudioEventConfigAssetBase>(movedAssets[i]);
                if (config == null)
                    continue;

                UnityAudioEventConfigAssetEditor.PathStatuses pathStatuses =
                    UnityAudioEventConfigAssetEditor.UpdateAudioEventConfigPath(config);
                
                if (pathStatuses == UnityAudioEventConfigAssetEditor.PathStatuses.NotInSpecifiedRootFolder)
                {
                    Debug.LogError($"You moved Unity Audio Event Config '{config.name}' outside of the specified " +
                                   $"root folder '{UnityAudioSyntaxSettings.Instance.AudioEventConfigAssetRootFolder}'. " +
                                   $"This Audio Event will no longer work correctly.", config);
                }
            }
        }
    }
}
