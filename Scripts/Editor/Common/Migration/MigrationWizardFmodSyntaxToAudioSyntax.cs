using System;
using System.Collections.Generic;
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
        
        private const string FmodSyntaxSystemName = "FmodSyntaxSystem";
        private const string GeneralSystemName = "AudioSyntaxSystem";
        private const string UnityAudioSystemName = "UnityAudioSyntaxSystem";
        private const string CullPlaybacksMethod = "CullPlaybacks";
        private const string StopAllActivePlaybacksMethod = "StopAllActivePlaybacks";
        private const string StopAllActiveEventPlaybacksMethod = "StopAllActiveEventPlaybacks";
        private const string UpdateMethod = "Update";
        
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

        private readonly Dictionary<string, string> outdatedSystemReferenceReplacements = new()
        {
            { $"{FmodSyntaxSystemName}.{CullPlaybacksMethod}", $"{GeneralSystemName}.{UpdateMethod}" },
            { $"{FmodSyntaxSystemName}.{StopAllActivePlaybacksMethod}",
                $"{GeneralSystemName}.{StopAllActivePlaybacksMethod}" },
            { $"{FmodSyntaxSystemName}.{StopAllActiveEventPlaybacksMethod}",
                $"{GeneralSystemName}.{StopAllActiveEventPlaybacksMethod}" },
        };
        
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
        
        private bool AreReplacementsNecessary(Dictionary<string, string> replacements)
        {
            foreach (KeyValuePair<string,string> oldTextNewTextPair in replacements)
            {
                if (IsContainedInScripts(oldTextNewTextPair.Key))
                    return true;
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
            
            hasDetectedOutdatedSystemReferences = AreReplacementsNecessary(outdatedSystemReferenceReplacements);
            
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
                EditorGUILayout.HelpBox($"There used to be one system called '{FmodSyntaxSystemName}'. This has " +
                                        $"been replaced by a general system '{GeneralSystemName}' which in turn " +
                                        $"updates both '{FmodSyntaxSystemName}' and '{UnityAudioSystemName}'. " +
                                        $"The '{CullPlaybacksMethod}' method has also been renamed " +
                                        $"to '{UpdateMethod}' because it now does more than just culling playbacks.",
                    MessageType.Error);
                
                bool shouldFixSystemReferencesAutomatically = GUILayout.Button("Fix Automatically");
                if (shouldFixSystemReferencesAutomatically)
                {
                    bool confirmed = EditorUtility.DisplayDialog(
                        "Automatically fix system references",
                        $"Are you sure you want to automatically update references to the old system " +
                        $"'{FmodSyntaxSystemName}' with references to the new system '{GeneralSystemName}' " +
                        $"where possible?\n\nWe recommend that you commit your changes to version control first so " +
                        $"that you don't lose any work.",
                        "Yes, I have saved my work.", "No");
                    
                    if (confirmed)
                        FixOutdatedSystemReferences();
                }
            }
            else
            {
                HelpBoxAffirmative($"There seem to be no more outdated references to '{FmodSyntaxSystemName}'.");
            }
            
            EndSettingsBox();
        }
        
        private void ReplaceInScripts(string oldText, string newText, bool partOfBatch = false)
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
            
            if (!partOfBatch)
                AssetDatabase.Refresh();
        }
        
        private void ReplaceInScripts(Dictionary<string, string> replacements)
        {
            foreach (KeyValuePair<string,string> oldTextNewTextPair in replacements)
            {
                ReplaceInScripts(oldTextNewTextPair.Key, oldTextNewTextPair.Value, true);
            }
            
            AssetDatabase.Refresh();
        }

        private void FixFmodSyntaxNamespaces()
        {
            ReplaceInScripts(FmodSyntaxNamespace, AudioSyntaxNamespace);
        }
        
        private void FixOutdatedSystemReferences()
        {
            ReplaceInScripts(outdatedSystemReferenceReplacements);
        }
    }
}
