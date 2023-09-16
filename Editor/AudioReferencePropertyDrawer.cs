using FMODUnity;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using GUID = FMOD.GUID;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Draws dropdowns for selecting an audio config. 
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioReference))]
    public class AudioReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // If this is an array, it would just show "Element 0" as the label which takes up a lot of space and is
            // useless. Even more useless is that it actually sees that the first property of AudioReference is a string
            // and shows that as the name instead for an array. That just means that it shows the GUID as the label.
            // HOW USEFUL. How about we don't show a label at all and just have a wider field that's easier to read?
            if (property.IsInArray())
                label = GUIContent.none;
            
            SerializedProperty audioConfigProperty = property.FindPropertyRelative("fmodEventGuid");
            
            // Figure out the display text and the content property depending on the mode.
            string displayedText;
            string guid = audioConfigProperty.stringValue;
            if (string.IsNullOrEmpty(guid))
            {
                displayedText = "";
            }
            else
            {
                GUID id = GUID.Parse(guid);
                EditorEventRef eventRef = EventManager.EventFromGUID(id);
                displayedText = eventRef.GetDisplayName();
            }

            // Draw a dropdown button to select the audio config.
            EditorGUI.BeginProperty(position, label, audioConfigProperty);
            Rect valueRect = EditorGUI.PrefixLabel(position, label);
            bool didPress = EditorGUI.DropdownButton(
                valueRect, new GUIContent(displayedText), FocusType.Keyboard);
            if (didPress)
            {
                Rect dropDownRect = position;
                dropDownRect.xMax += 200;
                
                AudioReferenceDropdown menu = new AudioReferenceDropdown(new AdvancedDropdownState(), audioConfigProperty);
                menu.Show(dropDownRect);
            }
            EditorGUI.EndProperty();
        }
    }
}
