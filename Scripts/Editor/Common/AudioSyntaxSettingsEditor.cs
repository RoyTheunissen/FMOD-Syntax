using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Draws a button for re-generating FMOD code.
    /// </summary>
    [CustomEditor(typeof(AudioSyntaxSettings))]
    public class AudioSyntaxSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            // Draw a button to generate code. This is particularly useful to have here because once you are done
            // tweaking the settings, you are likely to want to re-generate code anyway.
            EditorGUILayout.Space();
            bool shouldGenerateCode = GUILayout.Button("Generate Code", GUILayout.Height(40));
            if (shouldGenerateCode)
                AudioCodeGenerator.GenerateCode();
        }
    }
}
