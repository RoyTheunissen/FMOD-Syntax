using System;
using UnityEngine;

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
        [SerializeField] private ValueType value;

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
}
