using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    [CustomPropertyDrawer(typeof(AudioClipTimelineEvent))]
    public class AudioClipTimelineEventPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect labelRect = position.GetLabelRect(out Rect valueRect);
            valueRect.xMin += EditorGUIUtility.standardVerticalSpacing;
            
            SerializedProperty idProperty = property.FindPropertyRelative("id");
            SerializedProperty timeProperty = property.FindPropertyRelative("time");

            EditorGUI.PropertyField(labelRect, idProperty, GUIContent.none);

            // First figure out if this timeline event is relative to a specific audio clip.
            // If so we can draw a nice slider.
            AudioClip audioClip = null;
            SerializedProperty parentProperty = property.GetParent();
            string path = parentProperty.propertyPath;
            if (parentProperty != null)
            {
                parentProperty = parentProperty.GetParent();
                path = parentProperty.propertyPath;
                if (parentProperty != null)
                {
                    SerializedProperty audioClipProperty = parentProperty.FindPropertyRelative("audioClip");
                    if (audioClipProperty != null && audioClipProperty.objectReferenceValue != null)
                        audioClip = (AudioClip)audioClipProperty.objectReferenceValue;
                }
            }

            // If we know that this timeline event is for a specific audio clip, then draw a nice slider instead of a
            // float field because that's easier to work with.
            if (audioClip != null)
            {
                EditorGUI.BeginProperty(valueRect, GUIContent.none, timeProperty);
                timeProperty.floatValue = EditorGUI.Slider(valueRect, timeProperty.floatValue, 0.0f, audioClip.length);
                EditorGUI.EndProperty();
            }
            else
                EditorGUI.PropertyField(valueRect, timeProperty, GUIContent.none);
        }
    }
}
