#if UNITY_AUDIO_SYNTAX

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace RoyTheunissen.FMODSyntax.UnityAudioSyntax
{
    /// <summary>
    /// Config for a simple one-off sound effect as played back by Unity's native audio system.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioOneOffConfig", menuName = "ScriptableObject/Audio/One-Off Audio Config (Unity)")]
    public sealed class UnityAudioOneOffConfig : UnityAudioConfigGeneric<UnityAudioOneOffPlayback>
    {
        [Space]
        [SerializeField] private List<AudioClipMetaData> audioClips = new List<AudioClipMetaData>();
        public List<AudioClipMetaData> AudioClips => audioClips;

        [Space]
        [SerializeField] private bool randomizePitch;
        public bool RandomizePitch => randomizePitch;
        
        [SerializeField] private float randomPitchOffset = 0.1f;
        public float RandomPitchOffset => randomPitchOffset;

        [NonSerialized] private int lastRandomIndex;

        public AudioClipMetaData GetRandomClip()
        {
            if (audioClips.Count == 0)
                return null;

            int randomIndex = Random.Range(0, audioClips.Count);
            AudioClipMetaData randomClip = audioClips[randomIndex];
            
            // If we chose the same clip twice in a row, pick either the next or previous clip instead. This helps
            // prevent repetition of rapidly fired sound effects like footsteps.
            if (audioClips.Count > 1 && randomIndex == lastRandomIndex)
            {
                if (Random.Range(0, 100) < 50)
                    randomIndex = (randomIndex + 1).Modulo(audioClips.Count);
                else
                    randomIndex = (randomIndex - 1).Modulo(audioClips.Count);
            }
            
            lastRandomIndex = randomIndex;
            
            return randomClip;
        }
    }

    /// <summary>
    /// Responsible for the playback of a simple one-off sound as played back by Unity's native audio system.
    /// </summary>
    public class UnityAudioOneOffPlayback : UnityAudioPlaybackGeneric<UnityAudioOneOffConfig, UnityAudioOneOffPlayback>
    {
        /// <summary>
        /// Padding in seconds for how long an audio source will remain allocated to an audio playback after completion.
        /// To make sure nothing gets cut-off prematurely.
        /// </summary>
        private const float Padding = 0.05f;

        protected override bool ShouldFireEventsOnlyOnce => true;

        public override bool IsOneshot => true;

        protected override void OnStart()
        {
            AudioClipMetaData clip = Config.GetRandomClip();
            if (clip == null || clip.AudioClip == null)
            {
                Debug.LogError($"Audio config {Config} did not have a valid audio clip...");
                return;
            }
            Source.clip = clip;
            
            // Find the events associated with the clip that we decided to play, and add them to the list of events.
            if (clip.TimelineEvents != null)
                timelineEventsToFire.AddRange(clip.TimelineEvents);

            if (Config.RandomizePitch)
                Source.pitch = 1.0f + Random.Range(-Config.RandomPitchOffset, Config.RandomPitchOffset);
            else
                Source.pitch = 1.0f;
            
            // Can modify the volume specifically when invoking Play and also through the audio config.
            Source.volume = VolumeFactorOverride * Config.VolumeFactor;
            
            // Don't use PlayOneShot here but actually set the clip and call Play. This way you can still modify some
            // settings on the audio source immediately afterwards and have it apply correctly to the sound, like
            // reverb and roll-off settings.
            Source.Play();
        }

        public override void Update()
        {
            base.Update();
            
            float duration = Source.clip.length;
            
            time += Time.deltaTime;
            
            normalizedProgress = (time / duration).Saturate();

            // Make sure the sound comes from the specified transform.
            if (IsLocal && Origin != null)
                Source.transform.position = Origin.position;
                
            TryFiringRemainingEvents(timePrevious, time);
            
            timePrevious = time;

            if (time >= duration + Padding)
                Stop();
        }
        
        public UnityAudioOneOffPlayback SetVolume(float volume)
        {
            Volume = volume;
            return this;
        }

        protected override void OnStop()
        {
            MarkForCleanup();
        }
        
        protected override void OnCleanup()
        {
        }
        
        public override string ToString()
        {
            return Config.name + $" ({Source.clip.name})";
        }

        public void FadeOut(float duration)
        {
            // TODO: Add support for tweens back?
            // VolumeTween.TweenOut(duration);
        }
    }
}

#endif // UNITY_AUDIO_SYNTAX
