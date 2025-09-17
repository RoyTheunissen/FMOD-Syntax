using System;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Window to show to users when they haven't set up the FMOD Syntax system yet to let them conveniently
    /// initialize it with appropriate settings.
    /// </summary>
    public partial class MigrationWizard
    {
        private const string FmodSyntaxNamespace = "RoyTheunissen.FMODSyntax";
        private const string AudioSyntaxNamespace = "RoyTheunissen.AudioSyntax";
        private static readonly string[] AudioSyntaxBasePaths =
        {
            "FMOD-Syntax/",
            "FMOD-Syntax/",
            "com.roytheunissen.fmod-syntax/",
            "com.roytheunissen.audio-syntax/",
        };
        
        private const int VersionFmodSyntaxToAudioSyntax = 1;
        
        [NonSerialized] private bool hasDetectedIncorrectUsing;
        
        private bool IsScriptAllowedToReferenceOldNamespace(MonoScript monoScript)
        {
            string assetPath = AssetDatabase.GetAssetPath(monoScript);

            if (assetPath.StartsWith("Assets/"))
                assetPath = assetPath.RemoveAssetsPrefix();
            else if (assetPath.StartsWith("Packages/"))
                assetPath = assetPath.RemovePrefix("Packages/");
            
            // WE are allowed to reference it, for example in this very script :V
            for (int j = 0; j < AudioSyntaxBasePaths.Length; j++)
            {
                if (assetPath.StartsWith(AudioSyntaxBasePaths[j]))
                    return true;
            }

            return false;
        }
        
        private void DetectOutdatedNamespaceUsage()
        {
            hasDetectedIncorrectUsing = false;
            
            if (versionMigratingFrom >= VersionFmodSyntaxToAudioSyntax)
                return;
            
            MonoScript[] monoScripts = AssetLoading.GetAllAssetsOfType<MonoScript>();
            for (int i = 0; i < monoScripts.Length; i++)
            {
                // WE are allowed to reference it, for example in this very script :V
                if (IsScriptAllowedToReferenceOldNamespace(monoScripts[i]))
                    continue;
                
                string scriptText = monoScripts[i].text;
                bool hasIncorrectUsingInFile = scriptText.Contains(FmodSyntaxNamespace);
                if (hasIncorrectUsingInFile)
                {
                    hasDetectedIncorrectUsing = true;
                    return;
                }
            }
        }
        
        private void DrawMigrationFromFmodSyntaxToAudioSyntax()
        {
            if (versionMigratingFrom >= VersionFmodSyntaxToAudioSyntax)
                return;
            
            BeginSettingsBox("FMOD-Syntax to Audio-Syntax");
            EditorGUILayout.HelpBox("The system has since been updated to support Unity-based audio as well, and has been renamed from FMOD-Syntax to Audio-Syntax. Certain Namespaces / classes have been renamed, we need to make sure those are now updated if necessary.", MessageType.Info);

            if (hasDetectedIncorrectUsing)
            {
                EditorGUILayout.HelpBox($"The system has detected that the FMOD-Syntax namespace '{FmodSyntaxNamespace}' is being used. This has since been renamed to '{AudioSyntaxNamespace}'.", MessageType.Error);
                bool shouldFixNamespacesAutomatically = GUILayout.Button("Fix Automatically");
            }
            else
            {
                HelpBoxAffirmative($"There seem to be no more occurrences of the deprecated {FmodSyntaxNamespace} namespace.");
            }
            EndSettingsBox();
        }
    }
}
