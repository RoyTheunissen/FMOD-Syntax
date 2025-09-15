using RoyTheunissen.FMODSyntax.UnityAudioSyntax;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
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
            Rect foldoutRect = position.GetControlFirstRect();
            Rect eventsRect = foldoutRect.GetControlNextRect();
            
            // If we're in an array, don't bother drawing the label, otherwise it will just say Element 0.
            // Omit the label instead and create more space for the audio clip field, which is the most important thing.
            if (property.IsInArray())
                label = GUIContent.none;

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none);

            Rect audioClipRect = foldoutRect;
            EditorGUI.PropertyField(audioClipRect, property.FindPropertyRelative("audioClip"), label);
            
            if (property.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.PropertyField(eventsRect, property.FindPropertyRelative("events"));
                }
            }
        }
    }
}
