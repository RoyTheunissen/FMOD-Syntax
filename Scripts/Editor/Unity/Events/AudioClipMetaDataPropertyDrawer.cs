#if UNITY_AUDIO_SYNTAX

using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    [CustomPropertyDrawer(typeof(AudioClipMetaData))]
    public class AudioClipMetaDataPropertyDrawer : PropertyDrawer
    {
        private static readonly Color ErrorColor = Color.red;
        private static readonly Color ErrorTextColor = Color.Lerp(Color.white, ErrorColor, 0.15f);
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUI.GetPropertyHeight(property, label, true);
            
            SerializedProperty eventsProperty = property.FindPropertyRelative("timelineEvents");
            return RectExtensions.GetCombinedHeight(
                EditorGUI.GetPropertyHeight(eventsProperty, label, true), EditorGUIUtility.singleLineHeight);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty audioClipProperty = property.FindPropertyRelative("audioClip");
            SerializedProperty eventsProperty = property.FindPropertyRelative("timelineEvents");
            
            Rect foldoutRect = position.GetControlFirstRect();
            Rect eventsRect = foldoutRect.GetControlNextRect();
            
            // If we're in an array, don't bother drawing the label, otherwise it will just say Element 0.
            // Omit the label instead and create more space for the audio clip field, which is the most important thing.
            if (property.IsInArray())
                label = GUIContent.none;

            // Draw the header.
            bool wasExpanded = property.isExpanded;
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none);

            // If the header is expanded, also expand the events list because that's the only other thing to view,
            // so by definition you are interested in viewing the contents of the events list.
            if (!wasExpanded && property.isExpanded)
                eventsProperty.isExpanded = true;

            // Draw the audio clip in the header.
            Rect audioClipRect = foldoutRect;
            
            // Audio Clips are supposed to be assigned. If they aren't, make it very clear that something is wrong.
            Color originalBackgroundColor = GUI.backgroundColor;
            Color originalContentColor = GUI.contentColor;
            if (audioClipProperty.objectReferenceValue == null)
            {
                GUI.backgroundColor = ErrorColor;
                GUI.contentColor = ErrorTextColor;
            }
            
            EditorGUI.PropertyField(audioClipRect, audioClipProperty, label);

            GUI.backgroundColor = originalBackgroundColor;
            GUI.contentColor = originalContentColor;
            
            // Draw the events below, if the header is expanded.
            if (property.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                    EditorGUI.PropertyField(eventsRect, eventsProperty);
            }
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX
