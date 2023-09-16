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
            FmodParameterlessAudioPlayback instance = new FmodParameterlessAudioPlayback();
            instance.Play(EventDescription, source);
            return instance;
        }

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            FmodParameterlessAudioPlayback instance = new FmodParameterlessAudioPlayback();
            instance.Play(EventDescription, source);
            return instance;
        }
    }
}
