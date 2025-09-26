using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        [SerializeField] protected ValueType value;

        protected AudioEventConfigProperty(ValueType defaultValue)
        {
            value = defaultValue;
        }

        public ValueType Evaluate(UnityAudioPlayback playback)
        {
            // TODO: Evaluate our automation/modulation based on the playback's Parameter values 
            
            return value;
        }
    }
    
    [Serializable]
    public sealed class AudioEventConfigPropertyFloat : AudioEventConfigProperty<float>
    {
        [SerializeField] private bool isSigned;
        
        public AudioEventConfigPropertyFloat(float defaultValue, bool isSigned) : base(defaultValue)
        {
            this.isSigned = isSigned;
        }
    }
    
    [Serializable]
    public sealed class AudioEventConfigPropertyAudioClips : AudioEventConfigProperty<List<AudioClipMetaData>>
    {
        public bool HasAnythingAssigned
        {
            get
            {
                if (value == null)
                    return false;
                
                for (int i = 0; i < value.Count; i++)
                {
                    if (value[i] != null)
                        return true;
                }
                
                return false;
            }
        }
        
        [NonSerialized] private int lastRandomIndex;
        
        public AudioEventConfigPropertyAudioClips() : base(new List<AudioClipMetaData>())
        {
        }
        
        public AudioEventConfigPropertyAudioClips(List<AudioClipMetaData> defaultValue) : base(defaultValue)
        {
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
