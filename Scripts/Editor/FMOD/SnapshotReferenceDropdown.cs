#if FMOD_AUDIO_SYNTAX

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using FMODUnity;
using System.Linq;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Dropdown to pick snapshot references from a list in a searchable way / with subfolders.
    /// </summary>
    public sealed class SnapshotReferenceDropdown : AdvancedDropdown
    {
        private readonly SerializedProperty serializedProperty;

        public SnapshotReferenceDropdown(AdvancedDropdownState state, SerializedProperty serializedProperty)
            : base(state)
        {
            this.serializedProperty = serializedProperty;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            SnapshotConfigDropdownItem root = new SnapshotConfigDropdownItem(
                this, string.Empty, "Snapshots", string.Empty);

            EditorEventRef[] snapshots = EventManager.Events
                .Where(e => e.Path.StartsWith(EditorEventRefExtensions.SnapshotPrefix))
                .OrderBy(e => e.Path)
                .ToArray();
            string[] paths = new string[snapshots.Length];
            string[] guids = new string[snapshots.Length];
            
            root.AddChildByPath("None", "None");

            for (int i = 0; i < snapshots.Length; i++)
            {
                // Remove the event prefix otherwise it gets treated as a folder.
                string path = snapshots[i].Path;
                path = path.RemovePrefix(EditorEventRefExtensions.SnapshotPrefix);
                paths[i] = path;
                
                guids[i] = snapshots[i].Guid.ToString();
            }

            for (int i = 0; i < paths.Length; i++)
            {
                root.AddChildByPath(guids[i], paths[i]);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            SnapshotConfigDropdownItem dropdownItem = (SnapshotConfigDropdownItem)item;
            
            serializedProperty.serializedObject.Update();
            serializedProperty.stringValue = dropdownItem.Guid == "None" ? string.Empty : dropdownItem.Guid;
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }
    }
    
    public sealed class SnapshotConfigDropdownItem : AdvancedDropdownItem
    {
        private const char Separator = '/';
        
        private readonly SnapshotReferenceDropdown dropdown;

        private string path;
        
        private string guid;
        public string Guid => guid;

        public SnapshotConfigDropdownItem(
            SnapshotReferenceDropdown dropdown, string guid, string name, string path) : base(name)
        {
            this.dropdown = dropdown;
            this.guid = guid;
            this.path = path;
        }

        private SnapshotConfigDropdownItem AddChild(string guid, string name)
        {
            string newPath = string.IsNullOrEmpty(path) ? name : path + Separator + name;

            SnapshotConfigDropdownItem child = new SnapshotConfigDropdownItem(dropdown, guid, name, newPath);
            
            AddChild(child);
            
            return child;
        }

        private SnapshotConfigDropdownItem GetOrCreateChild(string guid, string name)
        {
            // Find a child with the specified name.
            foreach (AdvancedDropdownItem child in children)
            {
                if (child.name == name)
                    return (SnapshotConfigDropdownItem)child;
            }

            // Add a new one if it didn't exist yet.
            return AddChild(guid, name);
        }

        public void AddChildByPath(string guid, string relativePath)
        {
            // Leaf node, add the event there.
            if (!relativePath.Contains(Separator))
            {
                AddChild(guid, relativePath);
                return;
            }
            
            string[] sections = relativePath.Split(Separator);
            
            // Ensure a child exists for the first section of the path.
            // This acts as a folder and and does not have a key itself.
            SnapshotConfigDropdownItem firstSection = GetOrCreateChild(string.Empty, sections[0]);
            
            // Determine the path relative to this first child.
            string pathRemaining = string.Empty;
            for (int i = 1; i < sections.Length; i++)
            {
                pathRemaining += sections[i];

                if (i < sections.Length - 1)
                    pathRemaining += Separator;
            }
            
            // Continue recursively from the first section.
            firstSection.AddChildByPath(guid, pathRemaining);
        }
    }
}
#endif // FMOD_AUDIO_SYNTAX
