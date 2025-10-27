#if FMOD_AUDIO_SYNTAX

using UnityEngine;

namespace RoyTheunissen.AudioSyntax
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
        public abstract PlaybackType Play(Vector3 position);

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return Play(source);
        }
        
        IAudioPlayback IAudioConfig.Play(Vector3 position)
        {
            return Play(position);
        }
    }
}
#endif // FMOD_AUDIO_SYNTAX

