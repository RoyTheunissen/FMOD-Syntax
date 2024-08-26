using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Config for a playable FMOD audio event. Returns an instance of the specified AudioFmodPlayback type so you can
    /// modify its parameters. Configs are specified in FmodEvents.AudioEvents
    /// </summary>
    public abstract class FmodAudioConfig<PlaybackType> : FmodPlayableConfig, IAudioConfig
        where PlaybackType : FmodAudioPlayback
    {
        public abstract PlaybackType Play(Transform source = null);

        public FmodAudioConfig(string guid) : base(guid)
        {
        }

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return Play(source);
        }
    }
}
