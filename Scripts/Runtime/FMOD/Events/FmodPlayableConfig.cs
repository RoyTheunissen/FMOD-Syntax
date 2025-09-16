#if FMOD_AUDIO_SYNTAX

using System;
using FMOD;
using FMOD.Studio;
using FMODUnity;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Base class for configs of Playables (Events and Snapshots).
    /// </summary>
    public abstract class FmodPlayableConfig
    {
        [NonSerialized] private GUID id;
        public string Id => id.ToString();
        
        /// <summary>
        /// NOTE: Seems like we can't cache this for some reason that's related to domain reloading. Not sure yet why.
        /// </summary>
        protected EventDescription EventDescription => RuntimeManager.GetEventDescription(id);

        public string Path
        {
            get
            {
                EventDescription.getPath(out string path);
                return path;
            }
        }

        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        public bool IsAssigned => !id.IsNull;

        public FmodPlayableConfig(string guid)
        {
            id = GUID.Parse(guid);
        }

        public void Preload()
        {
            EventDescription.loadSampleData();
        }
        
        public void Unload()
        {
            EventDescription.unloadSampleData();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

#endif // FMOD_AUDIO_SYNTAX

