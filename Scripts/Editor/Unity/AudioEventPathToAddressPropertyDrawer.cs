#if UNITY_AUDIO_SYNTAX

using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    [CustomPropertyDrawer(typeof(AudioEventPathToAddress))]
    public class AudioEventPathToAddressPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty audioEventPathProperty = property.FindPropertyRelative("audioEventPath");
            SerializedProperty addressProperty = property.FindPropertyRelative("address");

            Rect labelRect = position.GetLabelRect(out Rect valueRect);

            const float indentCompensation = 14;
            labelRect.xMax -= indentCompensation;
            valueRect.xMin -= indentCompensation;

            EditorGUI.SelectableLabel(labelRect, audioEventPathProperty.stringValue, EditorStyles.boldLabel);
            
            EditorGUI.SelectableLabel(valueRect, addressProperty.stringValue);
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX
