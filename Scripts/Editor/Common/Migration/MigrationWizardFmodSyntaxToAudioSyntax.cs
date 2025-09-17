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
        
        private const string OldSystemName = "FmodSyntaxSystem";
        private const string NewGeneralSystemName = "AudioSyntaxSystem";
        private const string NewFmodSystemName = "FmodAudioSyntaxSystem";
        private const string RegisterEventPlaybackCallbackReceiverMethod = "RegisterEventPlaybackCallbackReceiver";
        private const string UnregisterEventPlaybackCallbackReceiverMethod = "UnregisterEventPlaybackCallbackReceiver";
        
        private static readonly string[] AudioSyntaxBasePaths =
        {
            "FMOD-Syntax/",
            "FMOD-Syntax/",
            "com.roytheunissen.fmod-syntax/",
            "com.roytheunissen.audio-syntax/",
        };
        
        private const int VersionFmodSyntaxToAudioSyntax = 1;
        
        [NonSerialized] private bool hasDetectedOutdatedNamespaceUsage;
        [NonSerialized] private bool hasDetectedOutdatedSystemReferences;
        
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
        
        private bool IsScriptInsideThisPackage(MonoScript monoScript)
        {
            string assetPath = AssetDatabase.GetAssetPath(monoScript);
            
            return IsProjectRelativePathInsideThisPackage(assetPath);
        }

        private bool IsContainedInScripts(string text)
        {
            MonoScript[] monoScripts = AssetLoading.GetAllAssetsOfType<MonoScript>();
            for (int i = 0; i < monoScripts.Length; i++)
            {
                // WE are allowed to reference it, for example in this very script :V
                if (IsScriptInsideThisPackage(monoScripts[i]))
                    continue;
                
                string scriptText = monoScripts[i].text;
                if (scriptText.Contains(text))
                {
                    return true;
                }
            }

            return false;
        }
        
        private void DetectOutdatedNamespaceUsage()
        {
            hasDetectedOutdatedNamespaceUsage = false;
            
            if (versionMigratingFrom >= VersionFmodSyntaxToAudioSyntax)
                return;

            hasDetectedOutdatedNamespaceUsage = IsContainedInScripts(FmodSyntaxNamespace);
            
            if (hasDetectedOutdatedNamespaceUsage)
                hasDetectedIssuesThatNeedToBeResolvedFirst = true;
        }
        
        private void DetectOutdatedSystemReferences()
        {
            hasDetectedOutdatedSystemReferences = false;
            
            if (versionMigratingFrom >= VersionFmodSyntaxToAudioSyntax)
                return;
            
            hasDetectedOutdatedSystemReferences = IsContainedInScripts(OldSystemName);
            
            if (hasDetectedOutdatedSystemReferences)
                hasDetectedIssuesThatNeedToBeResolvedFirst = true;
        }
        
        private void DrawMigrationFromFmodSyntaxToAudioSyntax()
        {
            if (versionMigratingFrom >= VersionFmodSyntaxToAudioSyntax)
                return;
            
            BeginSettingsBox("FMOD-Syntax to Audio-Syntax");
            
            EditorGUILayout.HelpBox("The system has since been updated to support Unity-based audio as well, and has been renamed from FMOD-Syntax to Audio-Syntax. Certain Namespaces / classes have been renamed, we need to make sure those are now updated if necessary.", MessageType.Info);
            
            EditorGUILayout.Space();

            if (hasDetectedOutdatedNamespaceUsage)
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
            
            EditorGUILayout.Space();
            
            if (hasDetectedOutdatedSystemReferences)
            {
                EditorGUILayout.HelpBox($"The system has detected that the deprecated system '{OldSystemName}' is being referenced. This has since been renamed to '{NewGeneralSystemName}'.", MessageType.Error);
                bool shouldFixNamespacesAutomatically = GUILayout.Button("Fix Automatically");
                if (shouldFixNamespacesAutomatically)
                {
                    bool confirmed = EditorUtility.DisplayDialog(
                        "Automatically fix deprecated system references",
                        $"Are you sure you want to automatically replace references to the old system " +
                        $"'{OldSystemName}' with references to the new system '{NewGeneralSystemName}'?\n\n" +
                        $"We recommend that you commit your changes to version control first so that you don't lose " +
                        $"any work.",
                        "Yes, I have saved my work.", "No");
                    
                    if (confirmed)
                        FixFmodSyntaxSystemReferences();
                }
            }
            else
            {
                HelpBoxAffirmative($"There seem to be no more references to the deprecated '{OldSystemName}' system.");
            }
            
            EndSettingsBox();
        }
        
        private void ReplaceInScripts(string oldText, string newText)
        {
            MonoScript[] monoScripts = AssetLoading.GetAllAssetsOfType<MonoScript>();
            for (int i = 0; i < monoScripts.Length; i++)
            {
                if (IsScriptInsideThisPackage(monoScripts[i]))
                    continue;
                
                string scriptText = monoScripts[i].text;
                bool hasIncorrectUsingInFile = scriptText.Contains(oldText);
                if (!hasIncorrectUsingInFile)
                    continue;

                scriptText = scriptText.Replace(oldText, newText);
                monoScripts[i].SetText(scriptText);
            }
            
            AssetDatabase.Refresh();
        }

        private void FixFmodSyntaxNamespaces()
        {
            ReplaceInScripts(FmodSyntaxNamespace, AudioSyntaxNamespace);
        }
        
        private void FixFmodSyntaxSystemReferences()
        {
            // Point existing calls to event playback (un)registration to the new FMOD-specific system which has a
            // slightly different name.
            // TODO: Or should we perhaps keep the old name so that this is not necessary to begin with?
            // 'FmodAudioSystaxSystem' is more consistent because there is now also 'UnityAudioSyntaxSystem'...
            ReplaceInScripts($"{OldSystemName}.{RegisterEventPlaybackCallbackReceiverMethod}", $"{NewFmodSystemName}.{RegisterEventPlaybackCallbackReceiverMethod}");
            ReplaceInScripts($"{OldSystemName}.{UnregisterEventPlaybackCallbackReceiverMethod}", $"{NewFmodSystemName}.{UnregisterEventPlaybackCallbackReceiverMethod}");
            
            ReplaceInScripts(OldSystemName, NewGeneralSystemName);
        }
    }
}
