using RoyTheunissen.FMODSyntax.UnityAudioSyntax;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    [CustomPropertyDrawer(typeof(AudioClipEvent))]
    public class AudioClipEventPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect labelRect = position.GetLabelRect(out Rect valueRect);
            SerializedProperty idProperty = property.FindPropertyRelative("id");
            SerializedProperty timeProperty = property.FindPropertyRelative("time");

            EditorGUI.PropertyField(labelRect, idProperty, GUIContent.none);
            EditorGUI.PropertyField(valueRect, timeProperty, GUIContent.none);
        }
    }
}
