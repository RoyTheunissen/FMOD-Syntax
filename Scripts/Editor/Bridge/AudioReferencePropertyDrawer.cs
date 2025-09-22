using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

#if FMOD_AUDIO_SYNTAX
using FMODUnity;
#endif // FMOD_AUDIO_SYNTAX

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Draws dropdowns for selecting an audio config. 
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioReference))]
    public class AudioReferencePropertyDrawer : PropertyDrawer
    {
#if FMOD_AUDIO_SYNTAX
        private string GetFmodDisplayText(bool supportsBothSystems, string fmodEventGuid)
        {
            if (string.IsNullOrEmpty(fmodEventGuid))
                return "";

            FMOD.GUID id = FMOD.GUID.Parse(fmodEventGuid);
            EditorEventRef eventRef = EventManager.EventFromGUID(id);
            string displayText = eventRef.GetDisplayName();
                    
            if (supportsBothSystems)
                displayText += " (FMOD)";

            return displayText;
        }
#endif // FMOD_AUDIO_SYNTAX
        
#if UNITY_AUDIO_SYNTAX
        private string GetUnityDisplayText(bool supportsBothSystems, UnityAudioEventConfigBase unityAudioEventConfig)
        {
            string displayText = unityAudioEventConfig == null ? "" : unityAudioEventConfig.name;
            if (supportsBothSystems)
                displayText += " (Unity)";
            return displayText;
        }
#endif // UNITY_AUDIO_SYNTAX
        
        private string GetDisplayText(
            bool supportsBothSystems, AudioReference.Modes mode, UnityAudioEventConfigBase unityAudioEventConfig,
            string fmodEventGuid)
        {
#if UNITY_AUDIO_SYNTAX && FMOD_AUDIO_SYNTAX
            switch (mode)
            {
                case AudioReference.Modes.Unity: return GetUnityDisplayText(supportsBothSystems, unityAudioEventConfig);

                case AudioReference.Modes.FMOD: return GetFmodDisplayText(supportsBothSystems, fmodEventGuid);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
#elif UNITY_AUDIO_SYNTAX
            return GetUnityDisplayText(supportsBothSystems, unityAudioEventConfig);
#elif FMOD_AUDIO_SYNTAX
            return GetFmodDisplayText(supportsBothSystems, fmodEventGuid);
#else
            return string.Empty;
#endif // FMOD_AUDIO_SYNTAX
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // If this is an array, it would just show "Element 0" as the label which takes up a lot of space and is
            // useless. Even more useless is that it actually sees that the first property of AudioReference is a string
            // and shows that as the name instead for an array. That just means that it shows the GUID as the label.
            // HOW USEFUL. How about we don't show a label at all and just have a wider field that's easier to read?
            if (property.IsInArray())
                label = GUIContent.none;
            
            SerializedProperty unityAudioEventConfigProperty = property.FindPropertyRelative("unityAudioEventConfig");
            SerializedProperty fmodAudioEventConfigProperty = property.FindPropertyRelative("fmodEventGuid");

            SerializedProperty audioConfigProperty;
            bool supportsBothSystems = false;
#if UNITY_AUDIO_SYNTAX && FMOD_AUDIO_SYNTAX
            if (unityAudioEventConfigProperty.objectReferenceValue != null)
                audioConfigProperty = unityAudioEventConfigProperty;
            else
                audioConfigProperty = fmodAudioEventConfigProperty;
            supportsBothSystems = true;
#elif UNITY_AUDIO_SYNTAX
            audioConfigProperty = unityAudioEventConfigProperty;
#elif FMOD_AUDIO_SYNTAX
            audioConfigProperty = fmodAudioEventConfigProperty;
#else
            audioConfigProperty = null;
            EditorGUI.LabelField(position, "Select at least one audio system.");
            return;
#endif
            
            SerializedProperty modeProperty = property.FindPropertyRelative("mode");
            
            // Figure out the display text and the content property.
            string displayedText = GetDisplayText(supportsBothSystems,
                (AudioReference.Modes)modeProperty.intValue,
                (UnityAudioEventConfigBase)unityAudioEventConfigProperty.objectReferenceValue,
                fmodAudioEventConfigProperty.stringValue);

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
                    new AdvancedDropdownState(), property.serializedObject, unityAudioEventConfigProperty,
                    fmodAudioEventConfigProperty, modeProperty);
                menu.Show(dropDownRect, 200);
            }
            EditorGUI.EndProperty();
        }
    }
}
