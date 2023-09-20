using System;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Window to show to users when they haven't set up the FMOD Syntax system yet to let them conveniently
    /// initialize it with appropriate settings.
    /// </summary>
    public class SetupWizard : EditorWindow
    {
        private string settingsFolderPath = string.Empty;
        private string generatedScriptsFolderPath = "Generated/Scripts/FMOD";
        
        private string namespaceForGeneratedCode;
        private bool shouldGenerateAssemblyDefinition = true;
        
        [NonSerialized] private GUIContent cachedFolderIcon;
        [NonSerialized] private bool didCacheFolderIcon;
        private GUIContent FolderIcon
        {
            get
            {
                if (!didCacheFolderIcon)
                {
                    didCacheFolderIcon = true;
                    string path = "Folder Icon";
                    if (EditorGUIUtility.isProSkin)
                        path = "d_" + path;
                    
                    cachedFolderIcon = EditorGUIUtility.IconContent(path, string.Empty);
                }
                return cachedFolderIcon;
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            string[] settingsAsset = AssetDatabase.FindAssets($"t:{nameof(FmodSyntaxSettings)}");
            if (settingsAsset.Length > 0)
                return;

            EditorApplication.delayCall += () =>
            {
                SetupWizard setupWizard = GetWindow<SetupWizard>(true, "FMOD Syntax Setup Wizard");
                setupWizard.minSize = setupWizard.maxSize = new Vector2(500, 270);
                setupWizard.namespaceForGeneratedCode = $"{Application.companyName}.{Application.productName}.FMOD";
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Welcome! You've installed the FMOD Syntax package.");
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Let's initialize the system with some default settings.");
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Folders", EditorStyles.boldLabel);

            settingsFolderPath = DrawFolderPathField(settingsFolderPath, "Settings Config", 
                "Where to create the config file that has all the settings in it.");

            generatedScriptsFolderPath = DrawFolderPathField(generatedScriptsFolderPath, "Generated Scripts", 
                "Where to place the generated scripts for events and such.");
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Code Formatting", EditorStyles.boldLabel);
            namespaceForGeneratedCode = EditorGUILayout.TextField(
                new GUIContent("Namespace", "The namespace to use for all generated code."), namespaceForGeneratedCode);

            GUIContent generateAsmdefLabel = new GUIContent(
                "Make Assembly Definition",
                "Whether to generate an assembly definition file for the generated FMOD code. " +
                "If the code is to be generated within one central runtime assembly definition for your game, " +
                "you may want to turn this off.");
            shouldGenerateAssemblyDefinition = EditorGUILayout.Toggle(
                generateAsmdefLabel, shouldGenerateAssemblyDefinition);
            
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();

            bool shouldInitialize = GUILayout.Button("Initialize", GUILayout.Height(40));
            if (shouldInitialize)
                InitializeFmodSyntaxSystem();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void InitializeFmodSyntaxSystem()
        {
            FmodSyntaxSettings settings = CreateScriptableObject<FmodSyntaxSettings>(
                settingsFolderPath, nameof(FmodSyntaxSettings));
            
            settings.InitializeFromWizard(
                generatedScriptsFolderPath, namespaceForGeneratedCode, shouldGenerateAssemblyDefinition);
            
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            
            Close();
        }

        private static void EnsureFolderExists(string path)
        {
            string absolutePath = Path.Combine(Application.dataPath, path);

            if (Directory.Exists(absolutePath))
                return;

            Directory.CreateDirectory(absolutePath);
            AssetDatabase.Refresh();
        }
        
        private static Type CreateScriptableObject<Type>(string path, string name)
            where Type : ScriptableObject
        {
            ScriptableObject scriptableObject = CreateInstance(typeof(Type));
            scriptableObject.name = name;
            
            EnsureFolderExists(path);

            path = Path.Combine(path, name + ".asset");
            AssetDatabase.CreateAsset(scriptableObject, Path.Combine("Assets", path));
            
            return (Type)scriptableObject;
        }

        private string DrawFolderPathField(string currentPath, string label, string tooltip = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(EditorGUIUtility.labelWidth));
            currentPath = EditorGUILayout.TextField(currentPath);
            float height = EditorGUIUtility.singleLineHeight;
            bool shouldPick = GUILayout.Button(FolderIcon, GUILayout.Width(1.5f * height), GUILayout.Height(height));
            if (shouldPick)
            {
                string selectedFolder = EditorUtility.OpenFolderPanel(
                    $"Pick {label} Folder", Path.Combine(Application.dataPath, currentPath), string.Empty);
                if (!string.IsNullOrEmpty(selectedFolder) && selectedFolder.StartsWith(Application.dataPath))
                {
                    selectedFolder = selectedFolder.Substring(Application.dataPath.Length);
                    
                    if (selectedFolder.StartsWith(Path.DirectorySeparatorChar) ||
                        selectedFolder.StartsWith(Path.AltDirectorySeparatorChar))
                    {
                        selectedFolder = selectedFolder.Substring(1);
                    }

                    currentPath = selectedFolder;
                    
                    EditorApplication.delayCall += Repaint;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            return currentPath;
        }
    }
}
