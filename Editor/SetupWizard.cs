using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using RoyTheunissen.FMODWrapper.Runtime;

namespace RoyTheunissen.FMODWrapper
{
    /// <summary>
    /// Window to show to users when they haven't set up the FMOD Wrapper system yet to let them conveniently
    /// initialize it with appropriate settings.
    /// </summary>
    public class SetupWizard : EditorWindow
    {
        private string settingsFolderPath = string.Empty;
        private string generatedScriptsFolderPath = "Generated/Scripts/FMOD";
        
        private string namespaceForGeneratedCode;
        
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
            string[] settingsAsset = AssetDatabase.FindAssets("t:FmodWrapperSettings");
            if (settingsAsset.Length > 0)
                return;
            
            SetupWizard setupWizard = GetWindow<SetupWizard>(true, "FMOD Wrapper Setup Wizard");
            setupWizard.minSize = setupWizard.maxSize = new Vector2(400, 248);
            setupWizard.namespaceForGeneratedCode = $"{Application.companyName}.{Application.productName}.FMOD";
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Welcome! You've installed the FMOD Wrapper package.");
            
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
            
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();

            bool shouldInitialize = GUILayout.Button("Initialize", GUILayout.Height(40));
            if (shouldInitialize)
                InitializeFmodWrapperSystem();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void InitializeFmodWrapperSystem()
        {
            FmodWrapperSettings settings = CreateScriptableObject<FmodWrapperSettings>(
                settingsFolderPath, nameof(FmodWrapperSettings));
            
            settings.InitializeFromWizard(generatedScriptsFolderPath, namespaceForGeneratedCode);
            
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
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            return currentPath;
        }
    }
}
