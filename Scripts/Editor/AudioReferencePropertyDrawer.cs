using System;
using FMODUnity;
using RoyTheunissen.FMODSyntax.UnityAudioSyntax;
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
        private string GetDisplayText(
            bool supportsBothSystems, AudioReference.Modes mode, UnityAudioConfigBase unityAudioConfig,
            string fmodEventGuid)
        {
            string displayText;
            switch (mode)
            {
                case AudioReference.Modes.Unity:
                    displayText = unityAudioConfig == null ? "" : unityAudioConfig.name;
                    if (supportsBothSystems)
                        displayText += " (Unity)";
                    break;

                case AudioReference.Modes.FMOD:
                    if (string.IsNullOrEmpty(fmodEventGuid))
                    {
                        displayText = "";
                        break;
                    }

                    GUID id = GUID.Parse(fmodEventGuid);
                    EditorEventRef eventRef = EventManager.EventFromGUID(id);
                    displayText = eventRef.GetDisplayName();
                    
                    if (supportsBothSystems)
                        displayText += " (FMOD)";
                    
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            return displayText;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // If this is an array, it would just show "Element 0" as the label which takes up a lot of space and is
            // useless. Even more useless is that it actually sees that the first property of AudioReference is a string
            // and shows that as the name instead for an array. That just means that it shows the GUID as the label.
            // HOW USEFUL. How about we don't show a label at all and just have a wider field that's easier to read?
            if (property.IsInArray())
                label = GUIContent.none;
            
            SerializedProperty unityAudioConfigProperty = property.FindPropertyRelative("unityAudioConfig");
            SerializedProperty fmodAudioConfigProperty = property.FindPropertyRelative("fmodEventGuid");

            SerializedProperty audioConfigProperty;
            bool supportsBothSystems = false;
#if UNITY_AUDIO_SYNTAX && FMOD_AUDIO_SYNTAX
            if (unityAudioConfigProperty.objectReferenceValue != null)
                audioConfigProperty = unityAudioConfigProperty;
            else
                audioConfigProperty = fmodAudioConfigProperty;
            supportsBothSystems = true;
#elif UNITY_AUDIO_SYNTAX
            audioConfigProperty = unityAudioConfigProperty;
#elif FMOD_AUDIO_SYNTAX
            audioConfigProperty = fmodAudioConfigProperty;
#else
            audioConfigProperty = null;
            EditorGUI.Label(position, "Select at least one audio system.");
            return;
#endif
            
            SerializedProperty modeProperty = property.FindPropertyRelative("mode");
            
            // Figure out the display text and the content property.
            string displayedText = GetDisplayText(supportsBothSystems,
                (AudioReference.Modes)modeProperty.intValue,
                (UnityAudioConfigBase)unityAudioConfigProperty.objectReferenceValue,
                fmodAudioConfigProperty.stringValue);

            Rect configRect = position;

            // Draw a dropdown button to select the audio config.
            EditorGUI.BeginProperty(configRect, label, audioConfigProperty);
            Rect valueRect = EditorGUI.PrefixLabel(configRect, label);
            bool didPress = EditorGUI.DropdownButton(
                valueRect, new GUIContent(displayedText), FocusType.Keyboard);
            if (didPress)
            {
                Rect dropDownRect = configRect;
                dropDownRect.xMax += 200;

                AudioReferenceDropdown menu = new(
                    new AdvancedDropdownState(), property.serializedObject, unityAudioConfigProperty,
                    fmodAudioConfigProperty, modeProperty);
                menu.Show(dropDownRect, 200);
            }
            EditorGUI.EndProperty();
        }
    }
}
