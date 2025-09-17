using System.Collections.Generic;
using UnityEditor;

namespace RoyTheunissen.AudioSyntax
{
    public partial class MigrationWizard
    {
        private static readonly string[] AudioSyntaxBasePaths =
        {
            "FMOD-Syntax/",
            "FMOD-Syntax/",
            "com.roytheunissen.fmod-syntax/",
            "com.roytheunissen.audio-syntax/",
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
            foreach (KeyValuePair<string, string> oldTextNewTextPair in replacements)
            {
                if (IsContainedInScripts(oldTextNewTextPair.Key))
                    return true;
            }

            return false;
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
            foreach (KeyValuePair<string, string> oldTextNewTextPair in replacements)
            {
                ReplaceInScripts(oldTextNewTextPair.Key, oldTextNewTextPair.Value, true);
            }

            AssetDatabase.Refresh();
        }
    }
}