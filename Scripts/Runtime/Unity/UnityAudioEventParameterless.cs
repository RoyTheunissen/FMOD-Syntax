#if UNITY_AUDIO_SYNTAX

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Config of a simple Unity Audio event without any parameters.
    /// </summary>
    public sealed class UnityAudioEventParameterlessConfig<ConfigType, PlaybackType>
        : UnityAudioEventStaticAccessConfig<ConfigType, PlaybackType>, IAudioConfigParameterless
        where ConfigType : UnityAudioEventConfigAssetBase
        where PlaybackType : UnityAudioPlayback, new()
    {
        public UnityAudioEventParameterlessConfig(string guid, string path, string name)
            : base(guid, path, name)
        {
        }
    }
}

#endif // UNITY_AUDIO_SYNTAX
