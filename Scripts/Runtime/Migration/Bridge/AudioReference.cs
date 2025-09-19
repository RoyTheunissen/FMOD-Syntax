#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using System;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    [Serializable]
    public class AudioReference : IAudioConfig
    {
        [SerializeField] private string fmodEventGuid;
        
        public bool IsAssigned => false;

        public string Name => string.Empty;
        public string Path => string.Empty;

        public FmodParameterlessAudioPlayback Play(Transform source = null)
        {
            return default;
        }
        
        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return Play(source);
        }
        
        
        public static FmodParameterlessAudioConfig GetParameterlessEventConfig(string guid)
        {
            return null;
        }
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
