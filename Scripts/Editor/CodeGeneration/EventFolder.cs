#if FMOD_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;

namespace RoyTheunissen.AudioSyntax
{
    public sealed class EventFolder
    {
        private readonly string name;
        public string Name => name;

        private readonly List<EventFolder> childFolders = new();
        public List<EventFolder> ChildFolders => childFolders;

        private readonly List<EditorEventRef> childEvents = new();
        public List<EditorEventRef> ChildEvents => childEvents;

        private readonly Dictionary<EditorEventRef, string> childEventToAliasPath = new();
        public Dictionary<EditorEventRef, string> ChildEventToAliasPath => childEventToAliasPath;

        public EventFolder(string name)
        {
            this.name = name;
        }

        private EventFolder GetOrCreateSubfolder(string name)
        {
            EventFolder existingFolder = 
                childFolders.FirstOrDefault(folder => string.Equals(folder.Name, name, StringComparison.Ordinal));
            
            if (existingFolder != null)
                return existingFolder;
            
            EventFolder newFolder = new(name);
            childFolders.Add(newFolder);
            childFolders.Sort((x, y) => String.Compare(x.name, y.name, StringComparison.Ordinal));
            return newFolder;
        }

        public EventFolder GetOrCreateChildFolderFromPathRecursively(string path)
        {
            string[] pathSections = path.Split("/");
            EventFolder currentFolder = this;
            
            // NOTE: The last section of the path is the event name.
            for (int i = 0; i < pathSections.Length - 1; i++)
            {
                currentFolder = currentFolder.GetOrCreateSubfolder(pathSections[i]);
            }

            return currentFolder;
        }

        public override string ToString()
        {
            return $"{nameof(EventFolder)}({Name})";
        }
    }
}
#endif // FMOD_AUDIO_SYNTAX
