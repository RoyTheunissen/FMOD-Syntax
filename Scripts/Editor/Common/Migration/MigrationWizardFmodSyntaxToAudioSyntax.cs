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

        private void DetectOutdatedNamespaceUsage()
        {
            hasDetectedOutdatedNamespaceUsage = false;
            
            if (versionMigratingFrom >= VersionFmodSyntaxToAudioSyntax)
                return;

            hasDetectedOutdatedNamespaceUsage = IsContainedInScripts(FmodSyntaxNamespace);
            
            if (hasDetectedOutdatedNamespaceUsage)
                ReportIssueThatNeedsToBeResolvedFirst();
        }
        
        private void DetectOutdatedSystemReferences()
        {
            hasDetectedOutdatedSystemReferences = false;
            
            if (versionMigratingFrom >= VersionFmodSyntaxToAudioSyntax)
                return;
            
            hasDetectedOutdatedSystemReferences = AreReplacementsNecessary(outdatedSystemReferenceReplacements);
            
            if (hasDetectedOutdatedSystemReferences)
                ReportIssueThatNeedsToBeResolvedFirst();
        }
        
        private void DrawMigrationFromFmodSyntaxToAudioSyntax()
        {
            if (versionMigratingFrom >= VersionFmodSyntaxToAudioSyntax)
                return;
            
            BeginSettingsBox("FMOD-Syntax to Audio-Syntax");
            
            EditorGUILayout.HelpBox("The system has since been updated to support Unity-based audio as well, and has " +
                                    "been renamed from FMOD-Syntax to Audio-Syntax. Certain Namespaces / classes " +
                                    "have been renamed, we need to make sure those are now updated if necessary.",
                MessageType.Info);
            
            EditorGUILayout.Space();

            if (hasDetectedOutdatedNamespaceUsage)
            {
                EditorGUILayout.HelpBox($"The system has detected that the FMOD-Syntax namespace " +
                                        $"'{FmodSyntaxNamespace}' is being used. This has since been renamed to " +
                                        $"'{AudioSyntaxNamespace}'.", MessageType.Error);
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
                HelpBoxAffirmative($"There seem to be no more occurrences of the deprecated " +
                                   $"{FmodSyntaxNamespace} namespace.");
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
