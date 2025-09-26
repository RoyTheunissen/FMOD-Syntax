using UnityEditor;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Draws a button for re-generating FMOD code.
    /// </summary>
    [CustomEditor(typeof(UnityAudioSyntaxSettings))]
    public class UnityAudioSyntaxSettingsEditor : Editor
    {
        private SerializedProperty unityAudioConfigRootFolderProperty;
        private SerializedProperty audioEventPathToAddressablePathsProperty;

        private void OnEnable()
        {
            unityAudioConfigRootFolderProperty = serializedObject.FindProperty("unityAudioConfigRootFolder");
            audioEventPathToAddressablePathsProperty = serializedObject.FindProperty("audioEventPathsToAddressablePaths");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            this.DrawFolderPathFieldLayout(unityAudioConfigRootFolderProperty);
            
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
