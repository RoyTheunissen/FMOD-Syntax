using RoyTheunissen.FMODSyntax.UnityAudioSyntax;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    [CustomPropertyDrawer(typeof(AudioLoopBookendConfig))]
    public class AudioLoopBookendConfigPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty enabledProperty = property.FindPropertyRelative("enabled");
            SerializedProperty audioClipProperty = property.FindPropertyRelative("audioClip");
            SerializedProperty volumeFactorProperty = property.FindPropertyRelative("volumeFactor");
            
            Rect headerRect = position.GetControlFirstRect();
            const float foldoutArrowSpace = 2;
            const float toggleWidth = 14;
            float spacing = 4;
            Rect enableRect = headerRect.GetSubRectFromLeft(foldoutArrowSpace + spacing + toggleWidth, out Rect headerLabelRect);
            enableRect.xMin += foldoutArrowSpace;
            Rect audioClipRect = headerRect.GetControlNextRect(EditorGUI.GetPropertyHeight(audioClipProperty, true));
            Rect volumeFactorRect = audioClipRect.GetControlNextRect();

            bool wasEnabled = enabledProperty.boolValue;
            EditorGUI.PropertyField(enableRect, enabledProperty, GUIContent.none);
            if (enabledProperty.boolValue && !wasEnabled)
                property.isExpanded = true;
            
            using (new EditorGUI.DisabledScope(!enabledProperty.boolValue))
            {
                property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, GUIContent.none);
                if (!enabledProperty.boolValue)
                    property.isExpanded = false;
                
                EditorGUI.LabelField(headerLabelRect, property.displayName, EditorStyles.boldLabel);
                if (property.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.PropertyField(audioClipRect, audioClipProperty, true);
                    EditorGUI.PropertyField(volumeFactorRect, volumeFactorProperty);
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
