using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using RoyTheunissen.AudioSyntax.UnityAudioSyntax;

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
        
        [Flags]
        public enum SupportedSystems
        {
            Unity = 1 << 0,
            FMOD = 1 << 1,
            Everything = ~0,
        }

        private readonly SerializedObject serializedObject;
        private readonly SerializedProperty unityAudioConfigProperty;
        private readonly SerializedProperty fmodAudioConfigProperty;
        private readonly SerializedProperty modeProperty;

        public AudioReferenceDropdown(
            AdvancedDropdownState state, SerializedObject serializedObject, SerializedProperty unityAudioConfigProperty,
            SerializedProperty fmodAudioConfigProperty, SerializedProperty modeProperty)
            : base(state)
        {
            this.serializedObject = serializedObject;
            this.unityAudioConfigProperty = unityAudioConfigProperty;
            this.fmodAudioConfigProperty = fmodAudioConfigProperty;
            this.modeProperty = modeProperty;
        }

        private static string[] GetGuidsOfAllAssetsOfType(Type type)
        {
            return AssetDatabase.FindAssets("t:" + type.Name);
        }
        
        private static string[] GetGuidsOfAllAssetsOfType<T>() where T : UnityEngine.Object
        {
            return GetGuidsOfAllAssetsOfType(typeof(T));
        }
        
        private static T[] GetAllAssetsOfType<T>() where T : UnityEngine.Object
        {
            string[] guids = GetGuidsOfAllAssetsOfType<T>();

            T[] result = new T[guids.Length];

            for (int i = 0; i < guids.Length; ++i)
            {
                result[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
            return result;
        }

        private string RemoveFirstDirectoryFromPath(string path)
        {
            int firstSlashIndex = path.IndexOf("/", StringComparison.OrdinalIgnoreCase);
            if (firstSlashIndex == -1)
                return path;

            return path.Substring(firstSlashIndex + 1);
        }

        private string GetDropdownPathForUnityAudioConfig(string assetPath, bool multipleAudioSystemsActive)
        {
            string dropdownPath = assetPath.RemoveSuffix(".asset");

            string basePath = AudioSyntaxSettings.Instance.UnityAudioConfigRootFolder.Path.ToUnityPath();
            if (!string.IsNullOrEmpty(basePath))
            {
                if (!basePath.EndsWith("/"))
                    basePath += "/";
                
                // Determine the path relative to the specified folder.
                dropdownPath = dropdownPath.RemovePrefix(basePath);
            }
            else
            {
                // No base folder was defined. Let's TRY and have some intelligent filtering.

                // No use in specifying that it's in the Assets folder. We know. 
                dropdownPath = dropdownPath.RemoveAssetsPrefix();

                // It's common for the main project folder to start with a _ or a [ so it is alphabetically first.
                // If that's the case here, then we know we're in the main folder and we can leave that part out too.
                // By the way, you should use _ instead of [ because Git LFS does not like it when you use [
                if (dropdownPath.StartsWith("_") || dropdownPath.StartsWith("["))
                    dropdownPath = RemoveFirstDirectoryFromPath(dropdownPath);
                
                // If we're in the main project folder, then check if we're in some kind of Configs folder.
                // At least, I know that that's a common structure to use.
                if (dropdownPath.StartsWith("Configs/") || dropdownPath.StartsWith("Configuration/") ||
                    dropdownPath.StartsWith("Configurations/") || dropdownPath.StartsWith("Data/")
                    || dropdownPath.StartsWith("Database/"))
                {
                    dropdownPath = RemoveFirstDirectoryFromPath(dropdownPath);
                }
                
                // It might also have a subfolder specifically for audio. We can filter that out because it's all audio.
                if (dropdownPath.StartsWith("Audio/"))
                    dropdownPath = RemoveFirstDirectoryFromPath(dropdownPath);
                
                // Now, if you use common Unity project structures, chances are you will get a short and reasonable
                // path. If not, well then specify the base folder yourself in the Audio Syntax Settings asset.
            }
            
            // Specify the audio system if multiple are active.
            if (multipleAudioSystemsActive)
                dropdownPath = UnitySectionName + "/" + dropdownPath;
            
            return dropdownPath;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AudioConfigDropdownItem root = new AudioConfigDropdownItem(
                this, string.Empty, "Audio Configs", string.Empty, SupportedSystems.Everything);
            
            SupportedSystems supportedSystems = 0;
#if UNITY_AUDIO_SYNTAX
            supportedSystems |= SupportedSystems.Unity;
#endif
#if FMOD_AUDIO_SYNTAX
            supportedSystems |= SupportedSystems.FMOD;
#endif
            
            root.AddChildByPath(NoneSelectedGuid, "None", SupportedSystems.Everything);
            
            bool multipleAudioSystemsActive = supportedSystems.HasFlag(SupportedSystems.Unity) &&
                                              supportedSystems.HasFlag(SupportedSystems.FMOD);
            
#if UNITY_AUDIO_SYNTAX
            if (supportedSystems.HasFlag(SupportedSystems.Unity))
            {
                UnityAudioConfigBase[] unityAudioConfigs = GetAllAssetsOfType<UnityAudioConfigBase>();
                string[] paths = new string[unityAudioConfigs.Length];
                string[] guids = new string[unityAudioConfigs.Length];

                for (int i = 0; i < unityAudioConfigs.Length; i++)
                {
                    string assetPath = AssetDatabase.GetAssetPath(unityAudioConfigs[i]);
                    
                    paths[i] = GetDropdownPathForUnityAudioConfig(assetPath, multipleAudioSystemsActive);

                    guids[i] = AssetDatabase.AssetPathToGUID(assetPath);
                }

                for (int i = 0; i < paths.Length; i++)
                {
                    root.AddChildByPath(guids[i], paths[i], SupportedSystems.Unity);
                }
            }
#endif
            
#if FMOD_AUDIO_SYNTAX
            if (supportedSystems.HasFlag(SupportedSystems.FMOD))
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
                    root.AddChildByPath(guids[i], paths[i], SupportedSystems.FMOD);
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
                case SupportedSystems.Unity:
                    modeProperty.intValue = (int)AudioReference.Modes.Unity;
                    break;
                
                case SupportedSystems.FMOD:
                    modeProperty.intValue = (int)AudioReference.Modes.FMOD;
                    break;
                
                case SupportedSystems.Everything:
                    // The updated applied to all systems. This happens if you select None. Mode needs no change.
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            // If the item selected is supposed to update the unity property, do so.
            // The GUID then represents the GUID of the UnityAudioConfigBase asset to load.
            if (dropdownItem.System.HasFlag(SupportedSystems.Unity) && !string.Equals(
                    dropdownItem.Guid, NoneSelectedGuid, StringComparison.OrdinalIgnoreCase))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(dropdownItem.Guid);
                UnityAudioConfigBase config = AssetDatabase.LoadAssetAtPath<UnityAudioConfigBase>(assetPath);
                unityAudioConfigProperty.objectReferenceValue = config;
            }
            else
            {
                unityAudioConfigProperty.objectReferenceValue = null;
            }
            
            // If the item selected is supposed to update the FMOD property, do so.
            // The GUID then represents the FMOD event GUID.
            if (dropdownItem.System.HasFlag(SupportedSystems.FMOD) && !string.Equals(
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

        private readonly AudioReferenceDropdown.SupportedSystems system;
        public AudioReferenceDropdown.SupportedSystems System => system;

        public AudioConfigDropdownItem(
            AudioReferenceDropdown dropdown, string guid, string name, string path,
            AudioReferenceDropdown.SupportedSystems system) : base(name)
        {
            this.dropdown = dropdown;
            this.guid = guid;
            this.path = path;
            this.system = system;
        }

        private AudioConfigDropdownItem AddChild(
            string guid, string name, AudioReferenceDropdown.SupportedSystems system)
        {
            string newPath = string.IsNullOrEmpty(path) ? name : path + Separator + name;

            AudioConfigDropdownItem child = new(dropdown, guid, name, newPath, system);
            
            AddChild(child);
            
            return child;
        }

        private AudioConfigDropdownItem GetOrCreateChild(
            string guid, string name, AudioReferenceDropdown.SupportedSystems system)
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

        public void AddChildByPath(string guid, string relativePath, AudioReferenceDropdown.SupportedSystems system)
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
