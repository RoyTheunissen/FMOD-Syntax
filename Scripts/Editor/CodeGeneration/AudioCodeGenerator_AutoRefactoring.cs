using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public static partial class AudioCodeGenerator
    {
        private static readonly char[] AllowedWordEndingCharacters = { '.', '(', ')', '[', ']', '{', '}', ';' };
        
        [MenuItem(RefactorOldEventReferencesMenuPath, false, 999999998)]
        private static void TryRefactoringOldEventReferences()
        {
            LoadPreviousMetaData();
            
            FindChangedEvents();
            
            TryRefactoringOldEventReferencesInternal(true);
        }
        
        [MenuItem(RefactorOldEventReferencesMenuPath, true, 999999998)]
        private static bool TryRefactoringOldEventReferencesValidate()
        {
            return File.Exists(PreviousMetaDataFilePath);
        }
        
        private static void FindChangedEvents()
        {
            EditorEventRef[] events = EventManager.Events
                .Where(e => e.Path.StartsWith(EditorEventRefExtensions.EventPrefix))
                .OrderBy(e => e.Path).ToArray();
            
            detectedEventChanges.Clear();
            foreach (EditorEventRef e in events)
            {
                bool eventExistedDuringPreviousCodeGeneration = metaDataFromPreviousCodeGeneration
                    .EventGuidToPreviousSyntaxPath.TryGetValue(e.Guid.ToString(), out string previousSyntaxPath);
                if (eventExistedDuringPreviousCodeGeneration)
                {
                    string currentPath = e.GetFilteredPath(true);
                    string currentSyntaxPath = GetEventSyntaxPath(currentPath);
                    
                    // Keep track of the event change we detected so we can try to automatically refactor existing
                    // code to reference the new path instead.
                    if (previousSyntaxPath != currentSyntaxPath)
                        detectedEventChanges[previousSyntaxPath] = currentSyntaxPath;
                }
            }
        }

        private static void TryRefactoringOldEventReferencesInternal(bool isTriggeredExplicitlyViaMenu)
        {
            const string messageTitle = "Auto-refactor references to changed events";
            
            if (detectedEventChanges.Count == 0)
            {
                if (isTriggeredExplicitlyViaMenu)
                {
                    EditorUtility.DisplayDialog(
                        messageTitle, "No events were detected as having been renamed or moved.", "OK");
                }
                return;
            }
            
            string messageText =   "FMOD-Syntax detected that events were renamed or moved. References to the " +
                                   "events in question will break. We can automatically try and refactor these " +
                                   "old references in your scripts. We recommend that you commit your changes to " +
                                   "version control first so that you don't lose any work.";
            if (!isTriggeredExplicitlyViaMenu)
                messageText += "\n\nYou can access this feature later via " + RefactorOldEventReferencesMenuPath + ".";
            
            const string yesText = "Auto-refactor";
            const string noText = "Cancel";
            bool autoRefactor = EditorUtility.DisplayDialog(messageTitle, messageText, yesText, noText);
            if (!autoRefactor)
                return;

            RefactorOldEventReferences(EventContainerClass);
            RefactorOldEventReferences(SnapshotContainerClass);
        }

        const string EventContainerClass = "AudioEvents";
        const string SnapshotContainerClass = "AudioSnapshots";

        private static string GetEventConfigType(
            string eventSyntaxPath, AudioSyntaxSettings.SyntaxFormats syntaxFormat, string containerName)
        {
            const string configSuffix = "Config";
            if (syntaxFormat == AudioSyntaxSettings.SyntaxFormats.SubclassesPerFolder)
                return containerName + "." + eventSyntaxPath + configSuffix;

            return eventSyntaxPath + configSuffix;
        }

        private static string GetEventPlaybackType(string eventSyntaxPath, AudioSyntaxSettings.SyntaxFormats syntaxFormat)
        {
            const string playbackSuffix = "Playback";
            if (syntaxFormat == AudioSyntaxSettings.SyntaxFormats.SubclassesPerFolder)
                return EventContainerClass + "." + eventSyntaxPath + playbackSuffix;

            return eventSyntaxPath + playbackSuffix;
        }
        
        private static string GetEventField(string eventSyntaxPath, string containerName)
        {
            return containerName + "." + eventSyntaxPath;
        }

        private static string ReplaceWords(string text, string oldWord, string newWord, out bool didReplace)
        {
            didReplace = false;

            int startIndex = text.IndexOf(oldWord, StringComparison.Ordinal);
            if (startIndex == -1)
                return text;

            bool IsAllowedChar(char c) => char.IsWhiteSpace(c) || AllowedWordEndingCharacters.Contains(c);

            int iterationCount = 0;
            const int maxIterationCount = 10000;
            while (startIndex != -1 && iterationCount < maxIterationCount)
            {
                int endIndex = startIndex + oldWord.Length;

                // Check if this occurrence is a whole word or if it's part of a longer word.
                bool isWholeWord = true;
                if (startIndex > 0)
                {
                    char previousCharacter = text[startIndex - 1];
                    if (!IsAllowedChar(previousCharacter))
                        isWholeWord = false;
                }

                if (isWholeWord && endIndex < text.Length)
                {
                    char nextCharacter = text[endIndex];
                    if (!IsAllowedChar(nextCharacter))
                        isWholeWord = false;
                }

                // Actually replace the occurrence with the new word.
                if (isWholeWord)
                {
                    string textPrecedingWord = text.Substring(0, startIndex);
                    string textSucceedingWord = text.Substring(endIndex);
                    text = textPrecedingWord + newWord + textSucceedingWord;

                    didReplace = true;

                    // Need to recompute the end index now that we modified the original text!
                    endIndex = startIndex + newWord.Length;
                }

                // Try to find the next occurrence of the word.
                startIndex = text.IndexOf(oldWord, endIndex, StringComparison.Ordinal);

                iterationCount++;
            }

            if (iterationCount >= maxIterationCount)
            {
                Debug.LogError(
                    $"Tried to replace word '{oldWord}' with '{newWord}' in text but something seemed to " +
                    $"have gone wrong because we did {maxIterationCount} loops and there should never be " +
                    $"that many occurrences. Exiting now to prevent a crash. If you genuinely have more " +
                    $"than {maxIterationCount} occurrences of '{oldWord}' in a text then you can try and " +
                    $"raise the limit and pray to whatever deity you hold dear because you clearly need " +
                    $"their support for whatever crazy high jinks you're up to.");
            }

            return text;
        }

        private static void RefactorOldEventReferences(string containerName)
        {
            AudioSyntaxSettings.SyntaxFormats oldSyntaxFormat = metaDataFromPreviousCodeGeneration.SyntaxFormat;
            AudioSyntaxSettings.SyntaxFormats newSyntaxFormat = Settings.SyntaxFormat;

            Dictionary<string, string> renamesToPerform = new();
            foreach (KeyValuePair<string, string> previousEventPathToNewPath in detectedEventChanges)
            {
                string oldSyntaxPath = previousEventPathToNewPath.Key;
                string newSyntaxPath = previousEventPathToNewPath.Value;

                // Rename references to the config type
                string oldConfigType = GetEventConfigType(oldSyntaxPath, oldSyntaxFormat, containerName);
                string newConfigType = GetEventConfigType(newSyntaxPath, newSyntaxFormat, containerName);
                if (!string.Equals(oldConfigType, newConfigType, StringComparison.Ordinal))
                    renamesToPerform[oldConfigType] = newConfigType;

                // Rename references to the playback type
                string oldPlaybackType = GetEventPlaybackType(oldSyntaxPath, oldSyntaxFormat);
                string newPlaybackType = GetEventPlaybackType(newSyntaxPath, newSyntaxFormat);
                if (!string.Equals(oldPlaybackType, newPlaybackType, StringComparison.Ordinal))
                    renamesToPerform[oldPlaybackType] = newPlaybackType;

                // Rename references to the field
                string oldFieldName = GetEventField(oldSyntaxPath, containerName);
                string newFieldName = GetEventField(newSyntaxPath, containerName);
                if (!string.Equals(oldFieldName, newFieldName, StringComparison.Ordinal))
                    renamesToPerform[oldFieldName] = newFieldName;
            }

#if LOG_FMOD_AUTO_RENAMES
            string log = "Intending to perform the following renames:\n";
            foreach (KeyValuePair<string,string> renameToPerform in renamesToPerform)
            {
                log += $"\t<b>{renameToPerform.Key}</b> => <b>{renameToPerform.Value}</b>\n";
            }
            Debug.Log(log);
#else
            string[] csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < csFiles.Length; i++)
            {
                string file = csFiles[i].ToUnityPath();

                string fileRelative = file.RemovePrefix(Application.dataPath + "/");

                // Just a sanity check, but don't refactor FMOD-Syntax itself...
                // The only thing that I could see it rename is examples in some of the comments.
                if (Refactor.IsProjectRelativePathInsideThisPackage(fileRelative))
                    continue;

                // Don't update the generated code itself. That one is already up to date.
                if (fileRelative.StartsWith(Settings.GeneratedScriptsFolderPath))
                    continue;

                // Perform the specified renames.
                try
                {
                    string fileText = File.ReadAllText(csFiles[i]);
                    bool didAnyRename = false;
                    foreach (KeyValuePair<string, string> renameToPerform in renamesToPerform)
                    {
                        // We do a special kind of replace that checks that the text in question is not part of some
                        // longer word. Helps prevent misfires like renaming fields of playback types.
                        fileText = ReplaceWords(
                            fileText, renameToPerform.Key, renameToPerform.Value, out bool didRename);
                        if (didRename)
                            didAnyRename = true;
                    }

                    if (didAnyRename)
                        File.WriteAllText(csFiles[i], fileText);
                }
                catch (Exception)
                {
                    Debug.LogWarning(
                        $"Could not update code file '{file}'. It may have been locked in an application.");
                    throw;
                }
            }

            AssetDatabase.Refresh();
#endif // !LOG_FMOD_AUTO_RENAMES
        }
    }
}
