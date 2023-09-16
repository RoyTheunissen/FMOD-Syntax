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
        private EventDescription eventDescription;
        protected EventDescription EventDescription => eventDescription;

        public abstract PlaybackType Play(Transform source = null);

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

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return Play(source);
        }
    }
}
