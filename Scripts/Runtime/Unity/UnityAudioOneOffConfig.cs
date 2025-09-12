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
        
        private Coroutine updateRoutine;

        private static WaitForSecondsRealtime paddingYieldInstruction = new WaitForSecondsRealtime(Padding);

        protected override bool ShouldFireEventsOnlyOnce => true;

        public override bool IsOneshot => true;

        private float normalizedProgress;
        public override float NormalizedProgress => normalizedProgress;

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
            if (clip.Events != null)
                eventsToFire.AddRange(clip.Events);

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

            // TODO: Come up with a solution for this that does not require us to copy over Routine
            // Routine.Start(ref updateRoutine, UpdateRoutine(clip));
        }

        private IEnumerator UpdateRoutine(AudioClip clip)
        {
            float time = 0.0f, timePrevious = 0.0f;
            float length = clip.length;
            while (time < length)
            {
                time += Time.deltaTime;
                normalizedProgress = (time / length).Saturate();

                // Make sure the sound comes from the specified transform.
                if (IsLocal && Origin != null)
                    Source.transform.position = Origin.position;
                
                TryFiringRemainingEvents(timePrevious, time);
                
                timePrevious = time;
                
                yield return null;
            }

            yield return paddingYieldInstruction;
            
            Stop();
        }

        protected override void OnStop()
        {
        }
        
        protected override void OnCleanup()
        {
            // TODO: Come up with a solution for this that does not require us to copy over Routine
            // Routine.Stop(ref updateRoutine);
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
