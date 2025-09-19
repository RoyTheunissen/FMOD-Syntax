#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

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
    [System.Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
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
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
