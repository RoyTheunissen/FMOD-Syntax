using System;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Draws a button for re-generating FMOD code.
    /// </summary>
    [CustomEditor(typeof(AudioSyntaxSettings))]
    public class AudioSyntaxSettingsEditor : Editor
    {
        private SerializedProperty audioSourcePooledPrefabProperty;
        private SerializedProperty defaultMixerGroupProperty;

        private void OnEnable()
        {
            audioSourcePooledPrefabProperty = serializedObject.FindProperty("audioSourcePooledPrefab");
            defaultMixerGroupProperty = serializedObject.FindProperty("defaultMixerGroup");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

#if UNITY_AUDIO_SYNTAX
            EditorGUILayout.PropertyField(audioSourcePooledPrefabProperty);
            EditorGUILayout.PropertyField(defaultMixerGroupProperty);
#endif // UNITY_AUDIO_SYNTAX
            
            serializedObject.ApplyModifiedProperties();
            
            // Draw a button to generate FMOD code. This is particularly useful to have here because once you are done
            // tweaking the settings, you are likely to want to re-generate code anyway.
            EditorGUILayout.Space();
            bool shouldGenerateCode = GUILayout.Button("Generate Code", GUILayout.Height(40));
            if (shouldGenerateCode)
                FmodCodeGenerator.GenerateCode();
        }
    }
}
