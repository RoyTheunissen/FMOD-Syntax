using System;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Config for a playable FMOD audio event. Returns an instance of the specified AudioFmodPlayback type so you can
    /// modify its parameters. Configs are specified in FmodEvents.Events
    /// </summary>
    public abstract class FmodAudioConfig<PlaybackType> : IAudioConfig
        where PlaybackType : FmodAudioPlayback
    {
        [NonSerialized] private GUID id;
        
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

        public abstract PlaybackType Play(Transform source = null);

        public FmodAudioConfig(string guid)
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

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return Play(source);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
