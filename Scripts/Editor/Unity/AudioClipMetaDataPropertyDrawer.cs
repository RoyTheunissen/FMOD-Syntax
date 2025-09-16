using RoyTheunissen.AudioSyntax.UnityAudioSyntax;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    [CustomPropertyDrawer(typeof(AudioClipMetaData))]
    public class AudioClipMetaDataPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
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
            EditorGUI.PropertyField(audioClipRect, audioClipProperty, label);
            
            // Draw the events below, if the header is expanded.
            if (property.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                    EditorGUI.PropertyField(eventsRect, eventsProperty);
            }
        }
    }
}
