using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Base class for a migration that the Migration Wizard can perform.
    /// For example migrating from FMOD-Syntax to Audio-Syntax.
    /// </summary>
    public abstract class Migration
    {
        public enum IssueUrgencies
        {
            Required,
            Optional,
        }
        
        private static readonly string[] AudioSyntaxBasePaths =
        {
            "FMOD-Syntax/",
            "FMOD-Syntax/",
            "com.roytheunissen.fmod-syntax/",
            "com.roytheunissen.audio-syntax/",
        };
        
        public abstract string DisplayName { get; }

        private int versionMigratingFrom;
        protected int VersionMigratingFrom => versionMigratingFrom;

        public abstract int VersionMigratingTo { get; }
        
        public abstract string Description { get; }
        
        public abstract string DocumentationURL { get; }
        
        public delegate void IssueDetectedHandler(Migration migration, IssueUrgencies urgency);
        public event IssueDetectedHandler IssueDetectedEvent;
        
        public delegate void RefactorPerformedHandler(Migration migration);
        public event RefactorPerformedHandler RefactorPerformedEvent;

        public void UpdateConditions(int versionMigratingFrom)
        {
            this.versionMigratingFrom = versionMigratingFrom;
            
            OnUpdateConditions();
        }

        protected abstract void OnUpdateConditions();

        public void OnGUI()
        {
            EditorGUILayout.HelpBox(Description, MessageType.Info);
            
            bool didClickDocumentation = EditorGUILayout.LinkButton("Documentation");
            if (didClickDocumentation)
                Application.OpenURL(DocumentationURL);
            
            EditorGUILayout.Space();
            
            DrawContents();
        }

        protected abstract void DrawContents();

        protected void ReportIssue(IssueUrgencies urgency)
        {
            IssueDetectedEvent?.Invoke(this, urgency);
        }
        
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

        protected bool IsContainedInScripts(string text)
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

        protected void ReportRefactorPerformed()
        {
            // TODO: Move this to Refactor class
            RefactorPerformedEvent?.Invoke(this);
        }
    }
}
