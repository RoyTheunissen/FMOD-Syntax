using UnityEditor;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Draws the audio event config asset root folder with a nice folder picker (this can apparently not be done
    /// with a property drawer).
    ///
    /// Also helps visualize which audio event paths map to which addresses.
    /// </summary>
    [CustomEditor(typeof(UnityAudioSyntaxSettings))]
    public class UnityAudioSyntaxSettingsEditor : Editor
    {
        private SerializedProperty audioEventConfigAssetRootFolderProperty;
        
        private SerializedProperty audioClipFoldersMirrorEventFoldersProperty;
        private SerializedProperty audioClipRootFolderProperty;
        
        private SerializedProperty audioEventPathToAddressablePathsProperty;

        private void OnEnable()
        {
            audioEventConfigAssetRootFolderProperty =
                serializedObject.FindProperty("audioEventConfigAssetRootFolder");

            audioClipFoldersMirrorEventFoldersProperty =
                serializedObject.FindProperty("audioClipFoldersMirrorEventFolders");
            audioClipRootFolderProperty = serializedObject.FindProperty("audioClipRootFolder");
            
            audioEventPathToAddressablePathsProperty =
                serializedObject.FindProperty("audioEventPathsToAddressablePaths");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            this.DrawFolderPathFieldLayout(audioEventConfigAssetRootFolderProperty);

            EditorGUILayout.PropertyField(audioClipFoldersMirrorEventFoldersProperty);
            if (audioClipFoldersMirrorEventFoldersProperty.boolValue)
                this.DrawFolderPathFieldLayout(audioClipRootFolderProperty);
            
#if UNITY_AUDIO_SYNTAX_ADDRESSABLES
            EditorGUILayout.Space();

            audioEventPathToAddressablePathsProperty.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(
                audioEventPathToAddressablePathsProperty.isExpanded,
                audioEventPathToAddressablePathsProperty.displayName);

            if (audioEventPathToAddressablePathsProperty.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    for (int i = 0; i < audioEventPathToAddressablePathsProperty.arraySize; i++)
                    {
                        SerializedProperty child = audioEventPathToAddressablePathsProperty.GetArrayElementAtIndex(i);
                        EditorGUILayout.PropertyField(child);
                    }
                }
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
#endif // UNITY_AUDIO_SYNTAX_ADDRESSABLES
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
