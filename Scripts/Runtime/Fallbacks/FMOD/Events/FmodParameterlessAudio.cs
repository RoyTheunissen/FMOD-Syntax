#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Playback of a simple FMOD event without any parameters.
    /// </summary>
    public sealed class FmodParameterlessAudioPlayback : FmodAudioPlayback
    {
    }

    /// <summary>
    /// Config of a simple FMOD event without any parameters.
    /// </summary>
    public sealed class FmodParameterlessAudioConfig : FmodAudioConfig<FmodParameterlessAudioPlayback>, IAudioConfig
    {
        public FmodParameterlessAudioConfig(string guid) : base(guid)
        {
        }

        public override FmodParameterlessAudioPlayback Play(Transform source = null)
        {
            return null;
        }

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return null;
        }
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
