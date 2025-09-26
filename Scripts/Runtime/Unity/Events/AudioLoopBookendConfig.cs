#if UNITY_AUDIO_SYNTAX

using System;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    [Serializable]
    public class AudioLoopBookendConfig
    {
        [SerializeField, HideInInspector] private bool enabled;
        
        [SerializeField] private AudioEventConfigPropertyAudioClips audioClips = new();
        public AudioEventConfigPropertyAudioClips AudioClips => audioClips;

        [SerializeField] private AudioEventConfigPropertyFloat volumeFactor = new(1.0f, false);
        public AudioEventConfigPropertyFloat VolumeFactor => volumeFactor;

        public bool ShouldPlay => enabled;
    }
}
#endif // UNITY_AUDIO_SYNTAX
