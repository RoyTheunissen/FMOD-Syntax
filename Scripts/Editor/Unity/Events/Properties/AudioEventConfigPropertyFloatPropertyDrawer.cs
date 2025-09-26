using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Draws a Float property as if it's a normal float.
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioEventConfigPropertyFloat))]
    public class AudioEventConfigPropertyFloatPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProperty = property.FindPropertyRelative("value");
            return EditorGUI.GetPropertyHeight(valueProperty, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty isSignedProperty = property.FindPropertyRelative("isSigned");
            bool isSigned = isSignedProperty.boolValue;
            
            SerializedProperty valueProperty = property.FindPropertyRelative("value");
            EditorGUI.PropertyField(position, valueProperty, label);
            if (!isSigned)
                valueProperty.floatValue = Mathf.Max(valueProperty.floatValue, 0.0f);
        }
    }
}
