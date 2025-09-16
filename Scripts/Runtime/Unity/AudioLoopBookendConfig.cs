#if UNITY_AUDIO_SYNTAX

using System;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    [Serializable]
    public class AudioLoopBookendConfig
    {
        [SerializeField, HideInInspector] private bool enabled;
        
        [SerializeField] private AudioClipMetaData audioClip;
        public AudioClipMetaData Clip => audioClip;

        [SerializeField] private float volumeFactor = 1.0f;
        public float VolumeFactor => volumeFactor;

        public bool ShouldPlay => enabled && audioClip != null;
    }
}
#endif // UNITY_AUDIO_SYNTAX
