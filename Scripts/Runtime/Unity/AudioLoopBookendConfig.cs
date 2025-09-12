using System;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax.UnityAudioSyntax
{
    [Serializable]
    public class AudioLoopBookendConfig
    {
        [SerializeField] private bool enabled;
        
        [SerializeField] private AudioClipMetaData audioClip;
        public AudioClipMetaData Clip => audioClip;

        [SerializeField] private float volumeFactor = 1.0f;
        public float VolumeFactor => volumeFactor;

        public bool ShouldPlay => enabled && audioClip != null;
    }
}
