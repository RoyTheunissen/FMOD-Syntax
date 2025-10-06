#if UNITY_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Custom editor for Unity audio events config assets. Mostly here to help ensure that paths are correctly because
    /// they are necessary for being able to load them via Addressables correctly.
    /// </summary>
    public abstract class UnityAudioEventConfigAssetEditor : Editor
    {
        public enum PathStatuses
        {
            Correct,
            CouldntDetermineRootFolder,
            NotInSpecifiedRootFolder,
            NotUpToDateWithAsset,
        }
        
        private SerializedProperty pathProperty;
        private SerializedProperty tagsProperty;
        
        [NonSerialized] private static GUIContent cachedPlayIcon;
        [NonSerialized] private static bool didCachePlayIcon;
        protected GUIContent PlayIcon
        {
            get
            {
                if (!didCachePlayIcon)
                {
                    didCachePlayIcon = true;
                    cachedPlayIcon = EditorGUIUtility.IconContent("PlayButton");
                }
                return cachedPlayIcon;
            }
        }
        
        [NonSerialized] private static GUIContent cachedStopIcon;
        [NonSerialized] private static bool didCacheStopIcon;
        
        [NonSerialized] private readonly List<KeyValuePair<string,object>> debugInformation = new();
        [NonSerialized] private UnityAudioPlayback lastPlayedPreview;

        protected GUIContent StopIcon
        {
            get
            {
                if (!didCacheStopIcon)
                {
                    didCacheStopIcon = true;
                    cachedStopIcon = EditorGUIUtility.IconContent("StopButton");
                }
                return cachedStopIcon;
            }
        }

        protected virtual void OnEnable()
        {
            pathProperty = serializedObject.FindProperty("path");
            tagsProperty = serializedObject.FindProperty("tags");
        }

        protected virtual void OnDisable()
        {
            StopAllPreviews();
        }
        
        protected virtual void StopAllPreviews()
        {
            lastPlayedPreview = null;
            
            if (!Application.isPlaying)
                AudioSyntaxSystem.StopAllActiveEventPlaybacks();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(pathProperty);

            UnityAudioEventConfigAssetBase config = target as UnityAudioEventConfigAssetBase;
            PathStatuses pathStatus = CheckIfPathIsCorrect(config, out string expectedPath);
            switch (pathStatus)
            {
                case PathStatuses.Correct:
                    break;
                    
                case PathStatuses.CouldntDetermineRootFolder:
                    EditorGUILayout.HelpBox("Could not determine what the root folder is for Unity Audio Events " +
                                            "because no Unity Audio Syntax Settings could be found.", MessageType.Error);
                    if (GUILayout.Button("Open Setup Wizard"))
                        SetupWizard.OpenSetupWizard();
                    break;
                    
                case PathStatuses.NotInSpecifiedRootFolder:
                    EditorGUILayout.HelpBox($"This audio event config asset is not a child of the root folder " +
                                            $"specified in the Audio Syntax Settings " +
                                            $"({UnityAudioSyntaxSettings.Instance.AudioEventConfigAssetRootFolder})",
                        MessageType.Error);
                    if (GUILayout.Button("Move To Specified Root Folder"))
                        MoveToEventRootFolder(config);
                    if (GUILayout.Button("Open Unity Audio Syntax Settings"))
                        UnityAudioSyntaxSettings.OpenSettings();
                    break;
                    
                case PathStatuses.NotUpToDateWithAsset:
                    if (string.IsNullOrEmpty(pathProperty.stringValue))
                    {
                        // If a path appeared to not have been set to begin with, just update it automatically.
                        // This is likely due to the asset being created for the first time, and we don't want that to
                        // always require an extra click from the user. 
                        pathProperty.stringValue = expectedPath;
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"The path does not appear to be up-to-date.\nCurrent path: " +
                                                $"'{pathProperty.stringValue}'\nExpected path: '{expectedPath}'",
                            MessageType.Error);
                        if (GUILayout.Button("Update All Audio Event Config Paths"))
                            UpdateAllAudioEventConfigAssetPaths();
                    }
                    break;
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            EditorGUILayout.Space();
            
            DrawPropertiesExcluding(serializedObject, "m_Script");
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(tagsProperty);
            
            serializedObject.ApplyModifiedProperties();
        }

        private void MoveToEventRootFolder(UnityAudioEventConfigAssetBase config)
        {
            string eventRootFolder = UnityAudioSyntaxSettings.Instance.AudioEventConfigAssetRootFolder;
            string originalPath = AssetDatabase.GetAssetPath(config);
            string fileName = Path.GetFileName(originalPath);

            string newPath = Path.Combine(eventRootFolder, fileName).ToUnityPath().AddAssetsPrefix();
            
            AssetDatabase.MoveAsset(originalPath, newPath);
            AssetDatabase.Refresh();
            
            EditorGUIUtility.PingObject(config);
        }

        public override bool HasPreviewGUI()
        {
#if UNITY_AUDIO_SYNTAX_DISABLE_PREVIEW
            return false;
#else
            return !Application.isPlaying;
#endif // UNITY_AUDIO_SYNTAX_DISABLE_PREVIEW
        }

        protected virtual bool IsPlayingPreview => false;

        public override void OnPreviewSettings()
        {
            base.OnPreviewSettings();
            
            GUIContent icon = IsPlayingPreview ? StopIcon : PlayIcon;
            if (GUILayout.Button(icon, EditorStyles.toolbarButton))
            {
                if (!IsPlayingPreview)
                {
                    PlayPreview();
                }
                else
                {
                    lastPlayedPreview.Stop();
                    
                    debugInformation.Clear();
                    lastPlayedPreview?.GetDebugInformation(debugInformation);

                    StopAllPreviews();
                }
            }
        }

        protected virtual void PlayPreview()
        {
            UnityAudioEventConfigAssetBase config = target as UnityAudioEventConfigAssetBase;

            debugInformation.Clear();
            lastPlayedPreview = config.PlayEditorPreview(debugInformation);
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            // Draw background
            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);
            
            UnityAudioEventLoopingConfigAsset config = target as UnityAudioEventLoopingConfigAsset;

            const float padding = 4;
            r.position += Vector2.one * padding;
            r.width -= padding * 2;
            r.height -= padding * 2;

            DrawPreviewInternal(r);
        }

        private void DrawPreviewInternal(Rect position)
        {
            position.yMin += 18;
            GUILayout.BeginArea(position);

            int index = 0;
            foreach (KeyValuePair<string,object> labelValuePair in debugInformation)
            {
                string label = labelValuePair.Key;
                object value = labelValuePair.Value;

                if (value is string text && string.Equals(text, "HEADER", StringComparison.Ordinal))
                {
                    if (index > 0)
                        EditorGUILayout.Space();
                        
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                    continue;
                }
                
                if (value is AudioClip || value == null)
                {
                    AudioClip obj = value as AudioClip;
                    EditorGUILayout.ObjectField(label, obj, typeof(AudioClip), false);
                    continue;
                }
                
                EditorGUILayout.LabelField(label, value.ToString(), EditorStyles.boldLabel);

                index++;
            }
            GUILayout.EndArea();
        }

        private static PathStatuses CheckIfPathIsCorrect(UnityAudioEventConfigAssetBase config, out string expectedPath)
        {
            string currentPath = config.Path;
            expectedPath = AssetDatabase.GetAssetPath(config).RemoveAssetsPrefix().RemoveSuffix(".asset");

            // This is a special use case, but if the path is null then this is for an asset that is still being
            // created. There's no point in throwing a fit over the path not being correct and confusing users
            // with a weird error message because we can't even ascertain what the correct path is right now.
            // Just report it as correct.
            if (string.IsNullOrEmpty(expectedPath))
                return PathStatuses.Correct;

            if (UnityAudioSyntaxSettings.Instance == null)
                return PathStatuses.CouldntDetermineRootFolder;

            string rootFolder =
                UnityAudioSyntaxSettings.Instance.AudioEventConfigAssetRootFolder.AddSuffixIfMissing("/");
            
            // Check if the asset is in the specified root folder.
            if (!expectedPath.StartsWith(rootFolder))
                return PathStatuses.NotInSpecifiedRootFolder;
            
            expectedPath = expectedPath.RemovePrefix(rootFolder);

            if (!string.Equals(currentPath, expectedPath, StringComparison.OrdinalIgnoreCase))
                return PathStatuses.NotUpToDateWithAsset;

            return PathStatuses.Correct;
        }
        
        public static void UpdateAllAudioEventConfigAssetPaths()
        {
            if (UnityAudioSyntaxSettings.Instance == null)
            {
                Debug.LogError($"Tried to automatically update all Unity Audio Event Config paths, " +
                               $"but there is no UnityAudioSyntaxSettings config to read the root path from.");
                return;
            }
            
            UnityAudioEventConfigAssetBase[] configs = AssetLoading.GetAllAssetsOfType<UnityAudioEventConfigAssetBase>();
            for (int i = 0; i < configs.Length; i++)
            {
                UpdateAudioEventConfigPathInternal(configs[i], true);
            }
        }
        
        private static PathStatuses UpdateAudioEventConfigPathInternal(
            UnityAudioEventConfigAssetBase config, bool logErrors)
        {
            string rootFolder = UnityAudioSyntaxSettings.Instance.AudioEventConfigAssetRootFolder;
            
            PathStatuses pathStatus = CheckIfPathIsCorrect(config, out string expectedPath);
            switch (pathStatus)
            {
                case PathStatuses.Correct:
                    break;
                    
                case PathStatuses.CouldntDetermineRootFolder:
                    // Already handled up above.
                    break;
                    
                case PathStatuses.NotInSpecifiedRootFolder:
                    if (logErrors)
                    {
                        Debug.LogError(
                            $"Tried to automatically update path for Unity Audio Event Config Asset " +
                            $"'{config.Name}' but it was not in the specified root folder '{rootFolder}'", config);
                    }
                    break;
                    
                case PathStatuses.NotUpToDateWithAsset:
                    // Wasn't up to date. Go make it up to date...
                    using (SerializedObject so = new(config))
                    {
                        so.Update();
                        SerializedProperty pathProperty = so.FindProperty("path");
                        pathProperty.stringValue = expectedPath;
                        so.ApplyModifiedProperties();
                    }
                    break;
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return pathStatus;
        }

        public static PathStatuses UpdateAudioEventConfigPath(UnityAudioEventConfigAssetBase config)
        {
            return UpdateAudioEventConfigPathInternal(config, false);
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX
