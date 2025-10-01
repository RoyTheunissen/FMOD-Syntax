#if UNITY_AUDIO_SYNTAX
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Useful for debugging.
    /// </summary>
    public struct LastPlayedAudioData
    {
        public bool HasPlayed;
        public AudioClip AudioClip;
        public float Volume;
        public float Pitch;
        
        private LastPlayedAudioData(bool hasPlayed, AudioClip audioClip, float volume, float pitch)
        {
            HasPlayed = hasPlayed;
            AudioClip = audioClip;
            Volume = volume;
            Pitch = pitch;
        }

        public LastPlayedAudioData(
            AudioClip audioClip, float volume, float pitch) : this(true, audioClip, volume, pitch)
        {
        }

        public LastPlayedAudioData(AudioSource audioSource)
            : this(audioSource.clip, audioSource.volume, audioSource.pitch)
        {
        }

        public static LastPlayedAudioData Default => new(false, null, 1.0f, 1.0f);
    }
}
#endif // UNITY_AUDIO_SYNTAX
