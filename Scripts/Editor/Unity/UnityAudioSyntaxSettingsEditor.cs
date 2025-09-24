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

        private void OnEnable()
        {
            unityAudioConfigRootFolderProperty = serializedObject.FindProperty("unityAudioConfigRootFolder");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            this.DrawFolderPathFieldLayout(unityAudioConfigRootFolderProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
