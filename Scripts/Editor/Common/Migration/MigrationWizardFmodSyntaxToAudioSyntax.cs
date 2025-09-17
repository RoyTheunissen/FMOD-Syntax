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
        
        public static bool IsProjectRelativePathInsideThisPackage(string projectRelativePath)
        {
            if (projectRelativePath.StartsWith("Assets/"))
                projectRelativePath = projectRelativePath.RemoveAssetsPrefix();
            else if (projectRelativePath.StartsWith("Packages/"))
                projectRelativePath = projectRelativePath.RemovePrefix("Packages/");
            
            // WE are allowed to reference it, for example in this very script :V
            for (int j = 0; j < AudioSyntaxBasePaths.Length; j++)
            {
                if (projectRelativePath.StartsWith(AudioSyntaxBasePaths[j]))
                    return true;
            }

            return false;
        }
        
        private bool IsScriptAllowedToReferenceOldNamespace(MonoScript monoScript)
        {
            string assetPath = AssetDatabase.GetAssetPath(monoScript);
            
            return IsProjectRelativePathInsideThisPackage(assetPath);
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
                if (shouldFixNamespacesAutomatically)
                {
                    bool confirmed = EditorUtility.DisplayDialog(
                        "Automatically fix namespaces",
                        $"Are you sure you want to automatically replace the {FmodSyntaxNamespace} namespace with " +
                        $"the {AudioSyntaxNamespace} namespace?\n\nWe recommend that you commit your changes to " +
                        $"version control first so that you don't lose any work.",
                        "Yes, I have saved my work.", "No");
                    
                    if (confirmed)
                        FixFmodSyntaxNamespaces();
                }
            }
            else
            {
                HelpBoxAffirmative($"There seem to be no more occurrences of the deprecated {FmodSyntaxNamespace} namespace.");
            }
            EndSettingsBox();
        }

        private void FixFmodSyntaxNamespaces()
        {
            MonoScript[] monoScripts = AssetLoading.GetAllAssetsOfType<MonoScript>();
            for (int i = 0; i < monoScripts.Length; i++)
            {
                if (IsScriptAllowedToReferenceOldNamespace(monoScripts[i]))
                    continue;
                
                string scriptText = monoScripts[i].text;
                bool hasIncorrectUsingInFile = scriptText.Contains(FmodSyntaxNamespace);
                if (!hasIncorrectUsingInFile)
                    continue;

                scriptText = scriptText.Replace(FmodSyntaxNamespace, AudioSyntaxNamespace);
                monoScripts[i].SetText(scriptText);
            }
            
            AssetDatabase.Refresh();
        }
    }
}
