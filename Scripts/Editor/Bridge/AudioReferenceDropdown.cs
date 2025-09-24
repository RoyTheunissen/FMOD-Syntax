using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Linq;

#if FMOD_AUDIO_SYNTAX
using FMODUnity;
#endif

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Dropdown to pick audio references from a list in a searchable way / with subfolders.
    /// </summary>
    public sealed class AudioReferenceDropdown : AdvancedDropdown
    {
        private const string FMODSectionName = "FMOD";
        private const string UnitySectionName = "Unity";
        private const string NoneSelectedGuid = "None";

        private readonly SerializedObject serializedObject;
        private readonly SerializedProperty unityAudioEventConfigProperty;
        private readonly SerializedProperty fmodAudioConfigProperty;
        private readonly SerializedProperty modeProperty;

        public AudioReferenceDropdown(
            AdvancedDropdownState state, SerializedObject serializedObject, SerializedProperty unityAudioEventConfigProperty,
            SerializedProperty fmodAudioConfigProperty, SerializedProperty modeProperty)
            : base(state)
        {
            this.serializedObject = serializedObject;
            this.unityAudioEventConfigProperty = unityAudioEventConfigProperty;
            this.fmodAudioConfigProperty = fmodAudioConfigProperty;
            this.modeProperty = modeProperty;
        }

        private string GetDropdownPathForUnityAudioEventConfig(
            UnityAudioEventConfigAssetBase config, bool multipleAudioSystemsActive)
        {
            string dropdownPath = UnityAudioSyntaxSettings.GetFilteredPathForUnityAudioEventConfig(config);
            
            // Specify the audio system if multiple are active.
            if (multipleAudioSystemsActive)
                dropdownPath = UnitySectionName + "/" + dropdownPath;
            
            return dropdownPath;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AudioConfigDropdownItem root = new AudioConfigDropdownItem(
                this, string.Empty, "Audio Configs", string.Empty, AudioSyntaxSystems.Everything);
            
            AudioSyntaxSystems supportedSystems = 0;
#if UNITY_AUDIO_SYNTAX
            supportedSystems |= AudioSyntaxSystems.UnityNativeAudio;
#endif
#if FMOD_AUDIO_SYNTAX
            supportedSystems |= AudioSyntaxSystems.FMOD;
#endif
            
            root.AddChildByPath(NoneSelectedGuid, "None", AudioSyntaxSystems.Everything);
            
            bool multipleAudioSystemsActive = supportedSystems.HasFlag(AudioSyntaxSystems.UnityNativeAudio) &&
                                              supportedSystems.HasFlag(AudioSyntaxSystems.FMOD);
            
#if UNITY_AUDIO_SYNTAX
            if (supportedSystems.HasFlag(AudioSyntaxSystems.UnityNativeAudio))
            {
                UnityAudioEventConfigAssetBase[] unityAudioEventConfigs =
                    AssetLoading.GetAllAssetsOfType<UnityAudioEventConfigAssetBase>();
                string[] paths = new string[unityAudioEventConfigs.Length];
                string[] guids = new string[unityAudioEventConfigs.Length];

                for (int i = 0; i < unityAudioEventConfigs.Length; i++)
                {
                    string assetPath = AssetDatabase.GetAssetPath(unityAudioEventConfigs[i]);

                    paths[i] = GetDropdownPathForUnityAudioEventConfig(
                        unityAudioEventConfigs[i], multipleAudioSystemsActive);

                    guids[i] = AssetDatabase.AssetPathToGUID(assetPath);
                }

                for (int i = 0; i < paths.Length; i++)
                {
                    root.AddChildByPath(guids[i], paths[i], AudioSyntaxSystems.UnityNativeAudio);
                }
            }
#endif
            
#if FMOD_AUDIO_SYNTAX
            if (supportedSystems.HasFlag(AudioSyntaxSystems.FMOD))
            {
                EditorEventRef[] parameterlessEvents = EventManager.Events
                    .Where(e => e.Path.StartsWith(EditorEventRefExtensions.EventPrefix) && e.LocalParameters.Count == 0)
                    .OrderBy(e => e.Path)
                    .ToArray();
                string[] paths = new string[parameterlessEvents.Length];
                string[] guids = new string[parameterlessEvents.Length];

                for (int i = 0; i < parameterlessEvents.Length; i++)
                {
                    // Remove the event prefix otherwise it gets treated as a folder.
                    string path = parameterlessEvents[i].Path;
                    path = path.RemovePrefix(EditorEventRefExtensions.EventPrefix);
                    
                    // Specify the audio system if multiple are active.
                    if (multipleAudioSystemsActive)
                        path = FMODSectionName + "/" + path;
                    
                    paths[i] = path;
                
                    guids[i] = parameterlessEvents[i].Guid.ToString();
                }

                for (int i = 0; i < paths.Length; i++)
                {
                    root.AddChildByPath(guids[i], paths[i], AudioSyntaxSystems.FMOD);
                }
            }
#endif

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            AudioConfigDropdownItem dropdownItem = (AudioConfigDropdownItem)item;
            
            serializedObject.Update();
            
            // If a property of a specific system was selected, update the mode to that system.
            switch (dropdownItem.System)
            {
                case AudioSyntaxSystems.UnityNativeAudio:
                    modeProperty.intValue = (int)AudioReference.Modes.Unity;
                    break;
                
                case AudioSyntaxSystems.FMOD:
                    modeProperty.intValue = (int)AudioReference.Modes.FMOD;
                    break;
                
                case AudioSyntaxSystems.Everything:
                    // The updated applied to all systems. This happens if you select None. Mode needs no change.
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            // If the item selected is supposed to update the unity property, do so.
            // The GUID then represents the GUID of the UnityAudioEventConfigAssetBase asset to load.
            if (dropdownItem.System.HasFlag(AudioSyntaxSystems.UnityNativeAudio) && !string.Equals(
                    dropdownItem.Guid, NoneSelectedGuid, StringComparison.OrdinalIgnoreCase))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(dropdownItem.Guid);
                UnityAudioEventConfigAssetBase eventConfig = AssetDatabase.LoadAssetAtPath<UnityAudioEventConfigAssetBase>(assetPath);
                unityAudioEventConfigProperty.objectReferenceValue = eventConfig;
            }
            else
            {
                unityAudioEventConfigProperty.objectReferenceValue = null;
            }
            
            // If the item selected is supposed to update the FMOD property, do so.
            // The GUID then represents the FMOD event GUID.
            if (dropdownItem.System.HasFlag(AudioSyntaxSystems.FMOD) && !string.Equals(
                    dropdownItem.Guid, NoneSelectedGuid, StringComparison.OrdinalIgnoreCase))
            {
                fmodAudioConfigProperty.stringValue = dropdownItem.Guid;
            }
            else
            {
                fmodAudioConfigProperty.stringValue = string.Empty;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
    
    public sealed class AudioConfigDropdownItem : AdvancedDropdownItem
    {
        private const char Separator = '/';
        
        private readonly AudioReferenceDropdown dropdown;

        private string path;
        
        private string guid;
        public string Guid => guid;

        private readonly AudioSyntaxSystems system;
        public AudioSyntaxSystems System => system;

        public AudioConfigDropdownItem(
            AudioReferenceDropdown dropdown, string guid, string name, string path,
            AudioSyntaxSystems system) : base(name)
        {
            this.dropdown = dropdown;
            this.guid = guid;
            this.path = path;
            this.system = system;
        }

        private AudioConfigDropdownItem AddChild(
            string guid, string name, AudioSyntaxSystems system)
        {
            string newPath = string.IsNullOrEmpty(path) ? name : path + Separator + name;

            AudioConfigDropdownItem child = new(dropdown, guid, name, newPath, system);
            
            AddChild(child);
            
            return child;
        }

        private AudioConfigDropdownItem GetOrCreateChild(
            string guid, string name, AudioSyntaxSystems system)
        {
            // Find a child with the specified name.
            AudioConfigDropdownItem existingChild = GetChild(name);
            if (existingChild != null)
                return existingChild;

            // Add a new one if it didn't exist yet.
            return AddChild(guid, name, system);
        }

        public AudioConfigDropdownItem GetChild(string name)
        {
            foreach (AdvancedDropdownItem child in children)
            {
                if (child.name == name)
                {
                    return (AudioConfigDropdownItem)child;
                }
            }

            return default;
        }

        public void AddChildByPath(string guid, string relativePath, AudioSyntaxSystems system)
        {
            // Leaf node, add the event there.
            if (!relativePath.Contains(Separator))
            {
                AddChild(guid, relativePath, system);
                return;
            }
            
            string[] sections = relativePath.Split(Separator);
            
            // Ensure a child exists for the first section of the path.
            // This acts as a folder and and does not have a key itself.
            AudioConfigDropdownItem firstSection = GetOrCreateChild(string.Empty, sections[0], system);
            
            // Determine the path relative to this first child.
            string pathRemaining = string.Empty;
            for (int i = 1; i < sections.Length; i++)
            {
                pathRemaining += sections[i];

                if (i < sections.Length - 1)
                    pathRemaining += Separator;
            }
            
            // Continue recursively from the first section.
            firstSection.AddChildByPath(guid, pathRemaining, system);
        }
    }
}
