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
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty isSignedProperty = property.FindPropertyRelative("isSigned");
            bool isSigned = isSignedProperty.boolValue;
            
            SerializedProperty valueProperty = property.FindPropertyRelative("value");
            if (!isSigned)
                valueProperty.floatValue = Mathf.Max(valueProperty.floatValue, 0.0f);
            
            Rect foldoutRect = position.GetControlFirstRect();

            // Draw the header.
            bool wasExpanded = property.isExpanded;
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none);

            // Draw the float value in the header.
            Rect audioClipRect = foldoutRect;
            EditorGUI.PropertyField(audioClipRect, valueProperty, label);
            
            // Draw the events below, if the header is expanded.
            if (property.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    const float foldoutArrowSpace = 2;
                    const float toggleWidth = 14;
                    float spacing = 4;
                    Rect randomOffsetRect = foldoutRect.GetControlNextRect();
                    randomOffsetRect.y += EditorGUIUtility.standardVerticalSpacing;

                    Rect enableRect = randomOffsetRect.GetSubRectFromLeft(foldoutArrowSpace + spacing + toggleWidth, out Rect headerLabelRect);
                    enableRect.xMin += foldoutArrowSpace;
                    
                    SerializedProperty applyRandomOffsetProperty = property.FindPropertyRelative("applyRandomOffset");
                    EditorGUI.PropertyField(enableRect, applyRandomOffsetProperty, GUIContent.none);

                    using (new EditorGUI.DisabledScope(!applyRandomOffsetProperty.boolValue))
                    {
                        SerializedProperty randomOffsetProperty = property.FindPropertyRelative("randomOffset");
                        const float labelInset = 20;
                        EditorGUIUtility.labelWidth -= labelInset;
                        EditorGUI.PropertyField(headerLabelRect, randomOffsetProperty);
                        EditorGUIUtility.labelWidth += labelInset;
                    }
                }
            }
        }
    }
}
