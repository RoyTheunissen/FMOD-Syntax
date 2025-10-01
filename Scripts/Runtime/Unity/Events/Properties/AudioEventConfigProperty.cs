#if UNITY_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Base class for properties inside audio event configs. The intention is to later add support for tying these to
    /// parameters via automation / modulation. I've already set them up so that the serialization structure won't
    /// have to be refactored completely to add this feature.
    /// </summary>
    [Serializable]
    public abstract class AudioEventConfigProperty 
    {
    }
    
    [Serializable]
    public abstract class AudioEventConfigProperty<ValueType>
    {
        [SerializeField, HideInInspector] protected ValueType value;

        protected AudioEventConfigProperty(ValueType defaultValue)
        {
            value = defaultValue;
        }

        public abstract ValueType Evaluate(UnityAudioPlayback playback);
    }
    
    [Serializable]
    public sealed class AudioEventConfigPropertyFloat : AudioEventConfigProperty<float>
    {
        [SerializeField, HideInInspector] private bool isSigned;
        
        [SerializeField, HideInInspector] private bool applyRandomOffset;

        [SerializeField] private float randomOffset = 0.1f;
        
        public AudioEventConfigPropertyFloat(float defaultValue, bool isSigned) : base(defaultValue)
        {
            this.isSigned = isSigned;
        }

        public override float Evaluate(UnityAudioPlayback playback)
        {
            // TODO: Support modulation / automation based on parameters
            float value = this.value;
            
            if (applyRandomOffset)
                value += Random.Range(-randomOffset, randomOffset);
            
            return value;
        }
    }
    
    [Serializable]
    public sealed class AudioEventConfigPropertyAudioClips : AudioEventConfigProperty<List<AudioClipMetaData>>
    {
        [NonSerialized] private int lastRandomIndex;
        
        public AudioEventConfigPropertyAudioClips() : base(new List<AudioClipMetaData>())
        {
        }
        
        public AudioEventConfigPropertyAudioClips(List<AudioClipMetaData> defaultValue) : base(defaultValue)
        {
        }

        public override List<AudioClipMetaData> Evaluate(UnityAudioPlayback playback)
        {
            // TODO: Support modulation / automation based on parameters
            return value;
        }

        public AudioClipMetaData GetAudioClipToPlay(UnityAudioPlayback playback)
        {
            List<AudioClipMetaData> clipsToSelectFrom = Evaluate(playback);
            
            if (clipsToSelectFrom.Count == 0)
                return null;

            int randomIndex = Random.Range(0, clipsToSelectFrom.Count);
            AudioClipMetaData randomClip = clipsToSelectFrom[randomIndex];
            
            // If we chose the same clip twice in a row, pick either the next or previous clip instead. This helps
            // prevent repetition of rapidly fired sound effects like footsteps.
            if (clipsToSelectFrom.Count > 1 && randomIndex == lastRandomIndex)
            {
                if (Random.Range(0, 100) < 50)
                    randomIndex = (randomIndex + 1).Modulo(clipsToSelectFrom.Count);
                else
                    randomIndex = (randomIndex - 1).Modulo(clipsToSelectFrom.Count);
            }
            
            lastRandomIndex = randomIndex;
            
            return randomClip;
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX
