using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEngine.Audio;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Window to show to users when they haven't set up the FMOD Syntax system yet to let them conveniently
    /// initialize it with appropriate settings.
    /// </summary>
    public class SetupWizard : WizardBase
    {
        private const int Priority = 0;
        
        private const float Width = 500;

        private const string ResourcesFolderSuffix = "Resources";
        
        private const string FmodScriptingDefineSymbol = "FMOD_AUDIO_SYNTAX";
        private const string UnityScriptingDefineSymbol = "UNITY_AUDIO_SYNTAX";

        [NonSerialized] private bool didDetectFMOD;
        [NonSerialized] private bool didDetectAudioSyntaxConfig;
        [NonSerialized] private AudioSyntaxSettings detectedAudioSyntaxConfig;
        [NonSerialized] private string detectedAudioSyntaxConfigPath;
        [NonSerialized] private bool isMigrationProcedureRequired;
        
        [NonSerialized] private bool didDetectUnityAudioSyntaxConfig;
        [NonSerialized] private UnityAudioSyntaxSettings detectedUnityAudioSyntaxConfig;
        [NonSerialized] private string detectedUnityAudioSyntaxConfigPath;
        
        private AudioSyntaxSystems activeSystems;
        
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
        private string unityAudioConfigRootFolder = string.Empty;

        private bool CanInitialize
        {
            get
            {
                if (activeSystems == 0)
                    return false;

                if (activeSystems.HasFlag(AudioSyntaxSystems.UnityNativeAudio)
                    && (audioSourcePooledPrefab == null || IsUnitySettingsFolderASubfolderOfResources()))
                {
                    return false;
                }

                return true;
            }
        }
        
        [NonSerialized] private static NamedBuildTarget[] cachedAllNamedBuildTargets;
        [NonSerialized] private static bool didCacheAllNamedBuildTargets;
        private static NamedBuildTarget[] AllNamedBuildTargets
        {
            get
            {
                if (!didCacheAllNamedBuildTargets)
                {
                    didCacheAllNamedBuildTargets = true;
                    
                    List<NamedBuildTarget> namedBuildTargets = new();

                    foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
                    {
                        if (group == BuildTargetGroup.Unknown)
                            continue;

                        NamedBuildTarget namedBuildTarget;
                        try
                        {
                            namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(group);
                        }
                        catch (ArgumentException)
                        {
                            continue;
                        }
                        
                        if (namedBuildTarget == NamedBuildTarget.Unknown)
                            continue;

                        if (!namedBuildTargets.Contains(namedBuildTarget))
                            namedBuildTargets.Add(namedBuildTarget);
                    }

                    cachedAllNamedBuildTargets = namedBuildTargets.ToArray();
                }
                return cachedAllNamedBuildTargets;
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

        [MenuItem(AudioSyntaxMenuPaths.Root + "Open Setup Wizard", false, Priority)]
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
                activeSystems |= AudioSyntaxSystems.FMOD;

            detectedAudioSyntaxConfig = TryFindConfig<AudioSyntaxSettings>(
                out didDetectAudioSyntaxConfig, out detectedAudioSyntaxConfigPath);

            isMigrationProcedureRequired = didDetectAudioSyntaxConfig &&
                                           detectedAudioSyntaxConfig.Version < AudioSyntaxSettings.CurrentVersion;

            detectedUnityAudioSyntaxConfig = TryFindConfig<UnityAudioSyntaxSettings>(
                out didDetectUnityAudioSyntaxConfig, out detectedUnityAudioSyntaxConfigPath);
            if (didDetectUnityAudioSyntaxConfig)
                activeSystems |= AudioSyntaxSystems.UnityNativeAudio;

            // If no audio source pooled prefab is specified, select the one included in the package.
            if (audioSourcePooledPrefab == null)
            {
                string audioSourcePooledPrefabDefaultPath =
                    AssetDatabase.GUIDToAssetPath("dbc94fd4dcfcddc42b214b0929284176");
                audioSourcePooledPrefab =
                    AssetDatabase.LoadAssetAtPath<AudioSource>(audioSourcePooledPrefabDefaultPath);
            }

            unityAudioConfigRootFolder = GetInferredUnityAudioConfigBasePathFromProjectStructure();
        }
        
        private static string GetInferredUnityAudioConfigBasePathFromProjectStructure()
        {
            // No base folder was defined. Let's TRY and have some intelligent filtering.

            string currentPath = Application.dataPath.ToUnityPath();

            // Check if there's a subdirectory that starts with a _ or a [. This is something Unity projects often have
            // in order to ensure that the main project files are sorted to be at the top. If we see that, it's
            // reasonable to assume that it's the main project folder.
            string[] rootSubDirectories = Directory.GetDirectories(currentPath);
            string directoryWithSymbol = rootSubDirectories.FirstOrDefault(sd =>
            {
                string folderName = Path.GetFileName(sd.TrimEnd(Path.AltDirectorySeparatorChar));
                return folderName.StartsWith("_") || folderName.StartsWith("[");
            });
            if (!string.IsNullOrEmpty(directoryWithSymbol))
                currentPath = directoryWithSymbol;

            void EnterSubdirectoryIfExists(params string[] names)
            {
                string[] subDirectories = Directory.GetDirectories(currentPath);
                for (int i = 0; i < subDirectories.Length; i++)
                {
                    string folderName = Path.GetFileName(subDirectories[i].TrimEnd(Path.AltDirectorySeparatorChar));
                    if (string.Equals(folderName, names[i], StringComparison.OrdinalIgnoreCase))
                    {
                        currentPath = subDirectories[i];
                        return;
                    }
                }
            }

            // If we're in the main project folder, then check if we're in some kind of Configs folder.
            // At least, I know that that's a common structure to use.
            EnterSubdirectoryIfExists("Configs", "Configuration", "Configurations", "Data", "Database");
            
            // Also check for an Audio subfolder, because you're likely to have other kinds of configs too.
            EnterSubdirectoryIfExists("Audio", "AudioSyntax", "Audio Syntax", "UnityAudioSyntax", "Unity Audio Syntax");

            string absolutePath = currentPath;
            string assetsFolderPath = Application.dataPath.RemoveSuffix("/");
            string relativePath = Path.GetRelativePath(assetsFolderPath, absolutePath).ToUnityPath();
            return relativePath;
        }

        private T TryFindConfig<T>(out bool didFindConfig, out string configPath)
            where T : ScriptableObject
        {
            string[] existingConfigPaths = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < existingConfigPaths.Length; i++)
            {
                existingConfigPaths[i] = AssetDatabase.GUIDToAssetPath(existingConfigPaths[i]);
            }
            didFindConfig = existingConfigPaths.Length > 0;
            configPath = didFindConfig ? existingConfigPaths[0].GetAbsolutePath() : null;
            return didFindConfig ? AssetDatabase.LoadAssetAtPath<T>(existingConfigPaths[0]) : null;
        }

        private static string SanitizeNamespace(string @namespace)
        {
            return FmodSyntaxUtilities.Filter(@namespace, false);
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
            
            if (activeSystems.HasFlag(AudioSyntaxSystems.UnityNativeAudio))
                DrawUnityAudioSpecificSettings();

            if (isMigrationProcedureRequired)
            {
                EditorGUILayout.HelpBox(MigrationWizard.MigrationNecessaryText, MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(!CanInitialize))
            {
                string buttonText = isMigrationProcedureRequired ? "Continue" : "Initialize";
                bool shouldInitialize = GUILayout.Button(buttonText, GUILayout.Height(40));
                if (shouldInitialize)
                    InitializeAudioSyntaxSystem();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAudioSystemSelection()
        {
            BeginSettingsBox("Audio System");
            
            EditorGUILayout.BeginHorizontal();
            const float systemsWidth = 150;
            EditorGUILayout.LabelField($"Which audio system(s) do you intend to use?", GUILayout.Width(Width - systemsWidth - 35));
            BeginValidityChecks(activeSystems != 0);
            activeSystems = (AudioSyntaxSystems)EditorGUILayout.EnumFlagsField(activeSystems, GUILayout.Width(systemsWidth));
            EndValidityChecks();
            EditorGUILayout.EndHorizontal();
            if (!didDetectFMOD && activeSystems.HasFlag(AudioSyntaxSystems.FMOD))
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
                using (new EditorGUI.DisabledScope(false))
                {
                    EditorGUILayout.ObjectField(detectedAudioSyntaxConfig, typeof(AudioSyntaxSettings), false);
                }
                EditorGUILayout.LabelField(detectedAudioSyntaxConfigPath, EditorStyles.miniLabel);
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
                using (new EditorGUI.DisabledScope(false))
                {
                    EditorGUILayout.ObjectField(detectedUnityAudioSyntaxConfig, typeof(UnityAudioSyntaxSettings), false);
                }
                EditorGUILayout.LabelField(detectedUnityAudioSyntaxConfigPath, EditorStyles.miniLabel);
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
                
                unityAudioConfigRootFolder = DrawFolderPathField(
                    unityAudioConfigRootFolder, "Audio Config Root Folder",
                    "Specifies which folder will contain the configs for all of your audio events. " +
                    "This is used to infer a path from the config.\n\n" +
                    "For example: 'Assets/_ProjectName/Configs/Audio/Player/Jump' will then be shortened to " +
                    "the more appropriate 'Player/Jump'.");
                EditorGUILayout.LabelField(unityAudioConfigRootFolder.GetAbsolutePath(), EditorStyles.miniLabel);
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
            
            if (!didDetectUnityAudioSyntaxConfig && activeSystems.HasFlag(AudioSyntaxSystems.UnityNativeAudio))
                CreateUnityAudioSyntaxSettingsFile();

            UpdateConfigWithSupportedAudioSystems();
            
            EnsureThatScriptingDefineSymbolsAreDefined(activeSystems);

            if (isMigrationProcedureRequired)
                MigrationWizard.OpenMigrationWizard();

            Close();
        }

        private void UpdateConfigWithSupportedAudioSystems()
        {
            AudioSyntaxSettings config = AudioSyntaxSettings.Instance;
            
            if (config.ActiveSystems == activeSystems)
                return;

            using (SerializedObject so = new(config))
            {
                so.Update();
                SerializedProperty activeSystemsProperty = so.FindProperty("activeSystems");
                activeSystemsProperty.intValue = (int)activeSystems;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private void CreateAudioSyntaxSettingsFile()
        {
            AudioSyntaxSettings settings = CreateScriptableObject<AudioSyntaxSettings>(
                settingsFolderPath, nameof(AudioSyntaxSettings));
            
            settings.InitializeFromWizard(
                activeSystems, generatedScriptsFolderPath, namespaceForGeneratedCode, shouldGenerateAssemblyDefinition);
            
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        
        private void CreateUnityAudioSyntaxSettingsFile()
        {
            string fileName = UnityAudioSyntaxSettings.SettingsFilename;
            string path = GetUnitySyntaxSettingsAssetFolderPath().RemoveSuffix(fileName);

            fileName = Path.GetFileNameWithoutExtension(fileName);
            UnityAudioSyntaxSettings settings = CreateScriptableObject<UnityAudioSyntaxSettings>(path, fileName);
            
            string rootPath = unityAudioConfigRootFolder.AddAssetsPrefix();
            
            settings.InitializeFromWizard(audioSourcePooledPrefab, defaultMixerGroup, rootPath);
            
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
        
        private static bool IsScriptingDefineSymbolDefined(string symbol)
        {
            NamedBuildTarget[] buildTargets = AllNamedBuildTargets;
            for (int i = 0; i < buildTargets.Length; i++)
            {
                PlayerSettings.GetScriptingDefineSymbols(buildTargets[i], out string[] symbols);
                if (!symbols.Contains(symbol))
                    return false;
            }

            return true;
        }
        
        private static bool IsScriptingDefineSymbolCorrect(string symbol, bool shouldExist)
        {
            return IsScriptingDefineSymbolDefined(symbol) == shouldExist;
        }

        public static bool AreScriptingDefineSymbolsCorrect(AudioSyntaxSystems systems)
        {
            bool isFmodCorrect = IsScriptingDefineSymbolCorrect(
                FmodScriptingDefineSymbol, systems.HasFlag(AudioSyntaxSystems.FMOD));
            bool isUnityCorrect = IsScriptingDefineSymbolCorrect(
                UnityScriptingDefineSymbol, systems.HasFlag(AudioSyntaxSystems.UnityNativeAudio));
            return isFmodCorrect || isUnityCorrect;
        }

        private static bool AddScriptingDefineSymbol(string symbol)
        {
            NamedBuildTarget[] buildTargets = AllNamedBuildTargets;
            for (int i = 0; i < buildTargets.Length; i++)
            {
                PlayerSettings.GetScriptingDefineSymbols(buildTargets[i], out string[] symbols);
                if (!symbols.Contains(symbol))
                {
                    symbols = symbols.Append(symbol).ToArray();
                    PlayerSettings.SetScriptingDefineSymbols(buildTargets[i], symbols);
                }
            }

            return true;
        }

        private static bool RemoveScriptingDefineSymbol(string symbol)
        {
            NamedBuildTarget[] buildTargets = AllNamedBuildTargets;
            for (int i = 0; i < buildTargets.Length; i++)
            {
                PlayerSettings.GetScriptingDefineSymbols(buildTargets[i], out string[] symbols);
                if (symbols.Contains(symbol))
                {
                    symbols = symbols.RemoveValue(symbol);
                    PlayerSettings.SetScriptingDefineSymbols(buildTargets[i], symbols);
                }
            }

            return true;
        }

        private static void SetScriptingDefineSymbol(string symbol, bool shouldBeSet)
        {
            if (shouldBeSet)
                AddScriptingDefineSymbol(symbol);
            else
                RemoveScriptingDefineSymbol(symbol);
        }
        
        public static void EnsureThatScriptingDefineSymbolsAreDefined(AudioSyntaxSystems systems)
        {
            SetScriptingDefineSymbol(FmodScriptingDefineSymbol, systems.HasFlag(AudioSyntaxSystems.FMOD));
            SetScriptingDefineSymbol(UnityScriptingDefineSymbol, systems.HasFlag(AudioSyntaxSystems.UnityNativeAudio));
        }
    }
}
