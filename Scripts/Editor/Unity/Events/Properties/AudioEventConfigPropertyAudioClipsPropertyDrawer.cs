#if UNITY_AUDIO_SYNTAX

using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Draws an Audio Clips property as if it's a normal list of audio clips.
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioEventConfigPropertyAudioClips))]
    public class AudioEventConfigPropertyAudioClipsPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProperty = property.FindPropertyRelative("value");
            return EditorGUI.GetPropertyHeight(valueProperty, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProperty = property.FindPropertyRelative("value");
            EditorGUI.PropertyField(position, valueProperty, label, true);
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX
