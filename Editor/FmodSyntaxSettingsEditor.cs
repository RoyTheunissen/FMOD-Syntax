using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Draws a button for re-generating FMOD code.
    /// </summary>
    [CustomEditor(typeof(FmodSyntaxSettings))]
    public class FmodSyntaxSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();
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
