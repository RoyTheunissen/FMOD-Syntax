#if FMOD_AUDIO_SYNTAX

using FMODUnity;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using GUID = FMOD.GUID;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Draws dropdowns for selecting a snapshot config. 
    /// </summary>
    [CustomPropertyDrawer(typeof(SnapshotReference))]
    public class SnapshotReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // If this is an array, it would just show "Element 0" as the label which takes up a lot of space and is
            // useless. Even more useless is that it actually sees that the first property of AudioReference is a string
            // and shows that as the name instead for an array. That just means that it shows the GUID as the label.
            // HOW USEFUL. How about we don't show a label at all and just have a wider field that's easier to read?
            if (property.IsInArray())
                label = GUIContent.none;
            
            SerializedProperty snapshotConfigProperty = property.FindPropertyRelative("fmodSnapshotGuid");
            
            // Figure out the display text and the content property.
            string displayedText;
            string guid = snapshotConfigProperty.stringValue;
            if (string.IsNullOrEmpty(guid))
            {
                displayedText = string.Empty;
            }
            else
            {
                GUID id = GUID.Parse(guid);
                EditorEventRef eventRef = EventManager.EventFromGUID(id);
                displayedText = eventRef == null ? string.Empty : eventRef.GetDisplayName();
            }

            // Draw a dropdown button to select the snapshot config.
            EditorGUI.BeginProperty(position, label, snapshotConfigProperty);
            Rect valueRect = EditorGUI.PrefixLabel(position, label);
            bool didPress = EditorGUI.DropdownButton(
                valueRect, new GUIContent(displayedText), FocusType.Keyboard);
            if (didPress)
            {
                Rect dropDownRect = position;
                dropDownRect.xMax += 200;
                
                SnapshotReferenceDropdown menu = new SnapshotReferenceDropdown(
                    new AdvancedDropdownState(), snapshotConfigProperty);
                menu.Show(dropDownRect);
            }
            EditorGUI.EndProperty();
        }
    }
}
#endif // FMOD_AUDIO_SYNTAX
