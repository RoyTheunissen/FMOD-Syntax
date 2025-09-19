using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Non-generic base class for FmodAudioConfig to apply as a type constraint.
    /// </summary>
    public abstract class FmodAudioConfigBase : FmodPlayableConfig
    {
        protected FmodAudioConfigBase(string guid) : base(guid)
        {
        }
    }
    
    /// <summary>
    /// Config for a playable FMOD audio event. Returns an instance of the specified AudioFmodPlayback type so you can
    /// modify its parameters. Configs are specified in FmodEvents.AudioEvents
    /// </summary>
    public abstract class FmodAudioConfig<PlaybackType> : FmodAudioConfigBase, IAudioConfig
        where PlaybackType : FmodAudioPlayback
    {
        public FmodAudioConfig(string guid) : base(guid)
        {
        }

        public abstract PlaybackType Play(Transform source = null);

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return Play(source);
        }
    }
}
