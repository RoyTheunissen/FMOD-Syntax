using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Audio;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Window to show to users when they haven't set up the FMOD Syntax system yet to let them conveniently
    /// initialize it with appropriate settings.
    /// </summary>
    public class SetupWizard : EditorWindow
    {
        [Flags]
        private enum SupportedSystems
        {
            UnityNativeAudio = 1 << 0,
            FMOD = 1 << 1,
        }

        private const float Width = 500;
        
        private static readonly Color WarningColor = Color.Lerp(Color.yellow, Color.red, 0.0f);
        private static readonly Color SuccessColor = Color.green;
        
        private const string ResourcesFolderSuffix = "Resources";

        [NonSerialized] private bool didDetectFMOD;
        [NonSerialized] private bool didDetectAudioSyntaxConfig;
        [NonSerialized] private string detectedAudioSyntaxConfig;
        [NonSerialized] private bool didDetectUnityAudioSyntaxConfig;
        [NonSerialized] private string detectedUnityAudioSyntaxConfig;
        
        private SupportedSystems supportedSystems;

        [NonSerialized] private Color preValidityCheckContentColor;
        [NonSerialized] private Color preValidityCheckBackgroundColor;
        
        [NonSerialized] private Color preWarningContentColor;
        [NonSerialized] private Color preWarningBackgroundColor;
        
        [NonSerialized] private Color preSuccessContentColor;
        [NonSerialized] private Color preSuccessBackgroundColor;
        
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
        
        private string settingsFolderPath = string.Empty;
        private string generatedScriptsFolderPath = "Generated/Scripts/Audio";
        
        private string namespaceForGeneratedCode;
        private bool shouldGenerateAssemblyDefinition = true;
        
        private string createUnitySyntaxSettingsAssetResourcesFolderPath = string.Empty;
        private AudioSource audioSourcePooledPrefab;
        private AudioMixerGroup defaultMixerGroup;

        private bool CanInitialize
        {
            get
            {
                if (supportedSystems == 0)
                    return false;

                if (supportedSystems.HasFlag(SupportedSystems.UnityNativeAudio)
                    && (audioSourcePooledPrefab == null || IsUnitySettingsFolderASubfolderOfResources()))
                {
                    return false;
                }

                return true;
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += () =>
            {
                // See if a Settings config exists. If not, the system is seemingly not initialized and we ought to
                // show the Setup Wizard.
                string[] settingsAsset = AssetDatabase.FindAssets($"t:{nameof(AudioSyntaxSettings)}");
                if (settingsAsset.Length > 0)
                    return;

                EditorApplication.delayCall += OpenSetupWizard;
            };
        }

        [MenuItem(AudioSyntaxMenuPaths.Root + "Open Setup Wizard")]
        public static void OpenSetupWizard()
        {
            SetupWizard setupWizard = GetWindow<SetupWizard>(true, AudioSyntaxMenuPaths.ProjectName + " Setup Wizard");
            setupWizard.minSize = setupWizard.maxSize = new Vector2(Width, 550);
            setupWizard.namespaceForGeneratedCode =
                $"{SanitizeNamespace(Application.companyName)}.{SanitizeNamespace(Application.productName)}.Audio";
        }

        private void OnEnable()
        {
            didDetectFMOD = AssetDatabase.AssetPathExists("Assets/Plugins/FMOD");
            if (didDetectFMOD)
                supportedSystems |= SupportedSystems.FMOD;

            TryFindConfig<AudioSyntaxSettings>(
                out didDetectAudioSyntaxConfig, out detectedAudioSyntaxConfig);

            TryFindConfig<UnityAudioSyntaxSettings>(
                out didDetectUnityAudioSyntaxConfig, out detectedUnityAudioSyntaxConfig);

            // If no audio source pooled prefab is specified, select the one included in the package.
            if (audioSourcePooledPrefab == null)
            {
                string audioSourcePooledPrefabDefaultPath =
                    AssetDatabase.GUIDToAssetPath("dbc94fd4dcfcddc42b214b0929284176");
                audioSourcePooledPrefab =
                    AssetDatabase.LoadAssetAtPath<AudioSource>(audioSourcePooledPrefabDefaultPath);
            }
        }

        private void TryFindConfig<T>(out bool didFindConfig, out string configPath)
            where T : ScriptableObject
        {
            string[] existingConfigPaths = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < existingConfigPaths.Length; i++)
            {
                existingConfigPaths[i] = AssetDatabase.GUIDToAssetPath(existingConfigPaths[i]);
            }
            didFindConfig = existingConfigPaths.Length > 0;
            configPath = didFindConfig ? existingConfigPaths[0].GetAbsolutePath() : null;
        }

        private static string SanitizeNamespace(string @namespace)
        {
            return FmodSyntaxUtilities.Filter(@namespace, false);
        }

        private void BeginValidityChecks(bool isValid)
        {
            preValidityCheckContentColor = GUI.contentColor;
            preValidityCheckBackgroundColor = GUI.backgroundColor;
            
            if (isValid)
                return;
            
            GUI.contentColor = Color.Lerp(Color.white, Color.red, 0.5f);
            GUI.backgroundColor = Color.red;
        }
        
        private void EndValidityChecks()
        {
            GUI.contentColor = preValidityCheckContentColor;
            GUI.backgroundColor = preValidityCheckBackgroundColor;
        }
        
        private void BeginWarning()
        {
            preWarningContentColor = GUI.contentColor;
            preWarningBackgroundColor = GUI.backgroundColor;
            
            GUI.contentColor = WarningColor;
            GUI.backgroundColor = WarningColor;
        }
        
        private void EndWarning()
        {
            GUI.contentColor = preWarningContentColor;
            GUI.backgroundColor = preWarningBackgroundColor;
        }
        
        private void BeginSuccess()
        {
            preSuccessContentColor = GUI.contentColor;
            preSuccessBackgroundColor = GUI.backgroundColor;
            
            GUI.contentColor = SuccessColor;
            GUI.backgroundColor = SuccessColor;
        }
        
        private void EndSuccess()
        {
            GUI.contentColor = preSuccessContentColor;
            GUI.backgroundColor = preSuccessBackgroundColor;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField($"Welcome! Let's initialize {AudioSyntaxMenuPaths.ProjectName} " +
                                       $"with the necessary settings.");
            
            EditorGUILayout.Space();
            
            DrawAudioSystemSelection();

            DrawGeneralSettings();
            
            if (supportedSystems.HasFlag(SupportedSystems.UnityNativeAudio))
                DrawUnityAudioSpecificSettings();

            using (new EditorGUI.DisabledScope(!CanInitialize))
            {
                bool shouldInitialize = GUILayout.Button("Initialize", GUILayout.Height(40));
                if (shouldInitialize)
                    InitializeAudioSyntaxSystem();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.EndHorizontal();
        }

        private void BeginSettingsBox(string title)
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
        }
        
        private void EndSettingsBox()
        {
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
        }

        private void DrawAudioSystemSelection()
        {
            BeginSettingsBox("Audio System");
            
            EditorGUILayout.BeginHorizontal();
            const float systemsWidth = 150;
            EditorGUILayout.LabelField($"Which audio system(s) do you intend to use?", GUILayout.Width(Width - systemsWidth - 35));
            BeginValidityChecks(supportedSystems != 0);
            supportedSystems = (SupportedSystems)EditorGUILayout.EnumFlagsField(supportedSystems, GUILayout.Width(systemsWidth));
            EndValidityChecks();
            EditorGUILayout.EndHorizontal();
            if (!didDetectFMOD && supportedSystems.HasFlag(SupportedSystems.FMOD))
            {
                BeginWarning();
                EditorGUILayout.LabelField($"Are you sure you wish to use FMOD? We did not detect it in your project.");
                EndWarning();
            }
            
            EndSettingsBox();
        }

        private void DrawGeneralSettings()
        {
            BeginSettingsBox("General System Settings File");
            
            if (didDetectAudioSyntaxConfig)
            {
                BeginSuccess();
                EditorGUILayout.LabelField($"Existing Audio Syntax Settings file detected \u2714");
                EndSuccess();
                EditorGUILayout.LabelField(detectedAudioSyntaxConfig, EditorStyles.miniLabel);
            }
            else
            {

                EditorGUILayout.LabelField("Folders", EditorStyles.boldLabel);

                settingsFolderPath = DrawFolderPathField(
                    settingsFolderPath, "Settings Config",
                    "Where to create the config file that has all the settings in it.");

                generatedScriptsFolderPath = DrawFolderPathField(
                    generatedScriptsFolderPath, "Generated Scripts",
                    "Where to place the generated scripts for events and such.");

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Code Formatting", EditorStyles.boldLabel);
                namespaceForGeneratedCode = EditorGUILayout.TextField(
                    new GUIContent("Namespace", "The namespace to use for all generated code."),
                    namespaceForGeneratedCode);

                GUIContent generateAsmdefLabel = new GUIContent(
                    "Make Assembly Definition",
                    "Whether to generate an assembly definition file for the generated FMOD code. " +
                    "If the code is to be generated within one central runtime assembly definition for your game, " +
                    "you may want to turn this off.");
                shouldGenerateAssemblyDefinition = EditorGUILayout.Toggle(
                    generateAsmdefLabel, shouldGenerateAssemblyDefinition);
            }

            EndSettingsBox();
        }

        private void DrawUnityAudioSpecificSettings()
        {
            BeginSettingsBox("Unity Audio Syntax Settings File");
            
            // TODO: REMOVE, JUST FOR TESTING
            // didDetectUnityAudioSyntaxConfig = true;
            // detectedUnityAudioSyntaxConfig = (Application.dataPath + "/Joe Mama.asset").ToUnityPath();

            if (didDetectUnityAudioSyntaxConfig)
            {
                BeginSuccess();
                EditorGUILayout.LabelField($"Existing Unity Audio Syntax Settings file detected \u2714");
                EndSuccess();
                EditorGUILayout.LabelField(detectedUnityAudioSyntaxConfig, EditorStyles.miniLabel);
            }
            else
            {
                // Decide where to create the asset
                EditorGUILayout.LabelField(
                    $"Where do you want to place the settings asset? It must be a Resources folder.");

                // Specify which Resources folder you want to place the asset in, but it must not be a subfolder of a 
                // Resources folder because then the Resources load would fail.
                bool isSubfolderOfResourcesFolder = IsUnitySettingsFolderASubfolderOfResources();
                BeginValidityChecks(!isSubfolderOfResourcesFolder);
                createUnitySyntaxSettingsAssetResourcesFolderPath = DrawFolderPathField(
                    createUnitySyntaxSettingsAssetResourcesFolderPath, "Config Resources Folder",
                    "Which Resources folder to create the Unity Audio Syntax Settings file inside that has all the " +
                    "settings in it.");
                if (isSubfolderOfResourcesFolder)
                {
                    EditorGUILayout.LabelField(
                        "Please specify a Resources folder, and not a subfolder of a Resources folder.",
                        EditorStyles.miniLabel);
                }
                EndValidityChecks();

                // Draw the current path, if it was valid.
                if (!isSubfolderOfResourcesFolder)
                {
                    string relativePath = GetUnitySyntaxSettingsAssetFolderPath();
                    string absolutePath = relativePath.GetAbsolutePath();
                    EditorGUILayout.LabelField(absolutePath, EditorStyles.miniLabel);
                }
                
                EditorGUILayout.Space();
                
                // Choose a Pooled Audio Source Prefab / Default Mixer Group
                BeginValidityChecks(audioSourcePooledPrefab != null);
                audioSourcePooledPrefab = (AudioSource)EditorGUILayout.ObjectField(
                    "Audio Source Prefab", audioSourcePooledPrefab, typeof(AudioSource), false);
                EndValidityChecks();
                
                defaultMixerGroup = (AudioMixerGroup)EditorGUILayout.ObjectField(
                    "Default Mixer Group", defaultMixerGroup, typeof(AudioMixerGroup), false);
            }
            
            EndSettingsBox();
        }

        private bool IsUnitySettingsFolderASubfolderOfResources()
        {
            string path = createUnitySyntaxSettingsAssetResourcesFolderPath.ToUnityPath();
            
            // Don't specify a subfolder of a Resources folder...
            if (path.Contains(ResourcesFolderSuffix + "/") && !path.EndsWith(ResourcesFolderSuffix + "/"))
                return true;

            return false;
        }

        private string GetUnitySyntaxSettingsAssetFolderPath()
        {
            string path = createUnitySyntaxSettingsAssetResourcesFolderPath.ToUnityPath();

            if (string.Equals(path, ResourcesFolderSuffix, StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, ResourcesFolderSuffix + "/", StringComparison.OrdinalIgnoreCase))
            {
                // Already good
            }
            else if (string.IsNullOrEmpty(path))
            {
                path += ResourcesFolderSuffix;
            }
            else if (!path.EndsWith("/" + ResourcesFolderSuffix))
            {
                if (!path.EndsWith("/"))
                    path += "/";
                
                path += ResourcesFolderSuffix;
            }

            if (!path.EndsWith("/"))
                path += "/";

            path += UnityAudioSyntaxSettings.PathRelativeToResources;
            return path;
        }

        private void InitializeAudioSyntaxSystem()
        {
            if (!didDetectAudioSyntaxConfig)
                CreateAudioSyntaxSettingsFile();
            
            if (!didDetectUnityAudioSyntaxConfig && supportedSystems.HasFlag(SupportedSystems.UnityNativeAudio))
                CreateUnityAudioSyntaxSettingsFile();

            Close();
        }

        private void CreateAudioSyntaxSettingsFile()
        {
            AudioSyntaxSettings settings = CreateScriptableObject<AudioSyntaxSettings>(
                settingsFolderPath, nameof(AudioSyntaxSettings));
            
            settings.InitializeFromWizard(
                generatedScriptsFolderPath, namespaceForGeneratedCode, shouldGenerateAssemblyDefinition);
            
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        
        private void CreateUnityAudioSyntaxSettingsFile()
        {
            string fileName = UnityAudioSyntaxSettings.SettingsFilename;
            string path = GetUnitySyntaxSettingsAssetFolderPath().RemoveSuffix(fileName);

            fileName = Path.GetFileNameWithoutExtension(fileName);
            UnityAudioSyntaxSettings settings = CreateScriptableObject<UnityAudioSyntaxSettings>(path, fileName);
            
            settings.InitializeFromWizard(audioSourcePooledPrefab, defaultMixerGroup);
            
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
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
