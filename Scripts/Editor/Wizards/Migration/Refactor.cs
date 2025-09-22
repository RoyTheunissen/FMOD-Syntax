using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Base class for one of several refactors that are part of a migration that the Migration Wizard can perform.
    /// For example renaming the namespaces from FMOD-Syntax to Audio-Syntax.
    /// </summary>
    public abstract class Refactor
    {
        private static readonly string[] AudioSyntaxBasePaths =
        {
            "FMOD-Syntax/",
            "FMOD-Syntax/",
            "com.roytheunissen.fmod-syntax/",
            "com.roytheunissen.audio-syntax/",
        };
        
        private bool isNecessary;
        public bool IsNecessary => isNecessary;

        private Migration.IssueUrgencies urgency;
        public Migration.IssueUrgencies Urgency => urgency;

        protected abstract string NotNecessaryDisplayText { get; }
        protected abstract string IsNecessaryDisplayText { get; }
        
        protected abstract string ConfirmationDialogueText { get; }

        public delegate void RefactorPerformedHandler(Refactor refactor);
        public static event RefactorPerformedHandler RefactorPerformedEvent;

        public void CheckIfNecessary()
        {
            isNecessary = CheckIfNecessaryInternal(out urgency);
        }

        protected abstract bool CheckIfNecessaryInternal(out Migration.IssueUrgencies urgency);
        
        private void PerformInternal(bool dispatchEvent)
        {
            OnPerform();

            if (dispatchEvent)
                RefactorPerformedEvent?.Invoke(this);
        }

        private void Perform()
        {
            PerformInternal(true);
        }

        public void PerformAsPartOfBatch()
        {
            // The intention was to not dispatch an event for every individual refactor when doing Auto Fix All,
            // but for robustness, perhaps it *is* best to re-evaluate all the refactors and see if they are still
            // necessary. Perhaps certain refactors will cause other refactors to not be necessary.
            PerformInternal(true);
        }

        protected abstract void OnPerform();

        public void OnGUI()
        {
            if (!isNecessary)
            {
                DrawingUtilities.HelpBoxAffirmative(NotNecessaryDisplayText);
                return;
            }

            EditorGUILayout.HelpBox(
                IsNecessaryDisplayText,
                Urgency == Migration.IssueUrgencies.Required ? MessageType.Error : MessageType.Warning);
            bool shouldPerform = GUILayout.Button("Fix Automatically");
            if (shouldPerform)
            {
                bool confirmed = EditorUtility.DisplayDialog("Automatic Refactor Confirmation",
                    ConfirmationDialogueText +
                    "\n\nWe recommend that you commit your changes to " +
                    $"version control first so that you don't lose any work.",
                    "Yes, I have saved my work.", "No");
                    
                if (confirmed)
                    Perform();
            }
        }

        // NOTE: This is also used in FmodCodeGenerator to ensure that event renames are not performed inside the
        // package itself. Generally speaking it is useful to exclude files to automatically refactor.
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
        
        protected bool IsReplacementNecessary(string from, string to)
        {
            return IsContainedInScripts(from);
        }

        protected bool AreReplacementsNecessary(Dictionary<string, string> replacements)
        {
            foreach (KeyValuePair<string, string> oldTextNewTextPair in replacements)
            {
                if (IsContainedInScripts(oldTextNewTextPair.Key))
                    return true;
            }

            return false;
        }

        protected void ReplaceInScripts(string oldText, string newText, bool partOfBatch = false)
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

        protected void ReplaceInScripts(Dictionary<string, string> replacements)
        {
            foreach (KeyValuePair<string, string> oldTextNewTextPair in replacements)
            {
                ReplaceInScripts(oldTextNewTextPair.Key, oldTextNewTextPair.Value, true);
            }

            AssetDatabase.Refresh();
        }

        protected string GetDisplayTextForReplacements(Dictionary<string, string> replacements)
        {
            string text = string.Empty;
            int count = replacements.Count;
            int index = 0;
            foreach (KeyValuePair<string,string> oldTextNewTextPair in replacements)
            {
                text += oldTextNewTextPair.Key + " \u2192 " + oldTextNewTextPair.Value;

                if (index < count - 1)
                    text += "\n";
                
                index++;
            }

            return text;
        }
    }
}
