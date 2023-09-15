using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace RoyTheunissen.FMODWrapper
{
    /// <summary>
    /// Config for a playable FMOD audio event. Returns an instance of the specified AudioFmodPlayback type so you can
    /// modify its parameters. Configs are specified in FmodEvents.Events
    /// </summary>
    public abstract class FmodAudioConfig<PlaybackType>
        where PlaybackType : FmodAudioPlayback
    {
        private EventDescription eventDescription;
        protected EventDescription EventDescription => eventDescription;

        public abstract PlaybackType Play(GameObject source = null);

        public FmodAudioConfig(string guid)
        {
            GUID id = GUID.Parse(guid);
            eventDescription = RuntimeManager.GetEventDescription(id);
        }

        public void Preload()
        {
            eventDescription.loadSampleData();
        }
        
        public void Unload()
        {
            eventDescription.unloadSampleData();
        }

        public PlaybackType Play(Transform source)
        {
            return Play(source == null ? null : source.gameObject);
        }
    }
}
