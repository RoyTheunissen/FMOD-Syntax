#if UNITY_AUDIO_SYNTAX

using UnityEngine;

namespace RoyTheunissen.FMODSyntax.UnityAudioSyntax
{
    /// <summary>
    /// Config for a looping sound effect as played back by Unity's native audio system.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLoopingConfig", menuName = "ScriptableObject/Audio/Audio Config (Looping)")]
    public class UnityAudioLoopingConfig : UnityAudioConfigGeneric<UnityAudioLoopingPlayback>
    {
        [Space]
        [SerializeField] private AudioLoopBookendConfig startAudio;
        public AudioLoopBookendConfig StartAudio => startAudio;
        
        [Header("Looping Audio")]
        [SerializeField] private AudioClipMetaData loopingAudioClip;
        public AudioClipMetaData LoopingAudioClip => loopingAudioClip;

        [Space]
        [SerializeField] private AudioLoopBookendConfig endAudio;
        public AudioLoopBookendConfig EndAudio => endAudio;
    }

    /// <summary>
    /// Responsible for the playback of a looping sound as played back by Unity's native audio system.
    /// </summary>
    public class UnityAudioLoopingPlayback : UnityAudioPlaybackGeneric<UnityAudioLoopingConfig, UnityAudioLoopingPlayback>
    {
        protected override bool ShouldFireEventsOnlyOnce => false;

        public override bool IsOneshot => false;

        protected override void OnStart()
        {
            if (Config.LoopingAudioClip == null)
            {
                Debug.LogError($"Audio loop config {Config} did not have a valid looping audio clip...");
                return;
            }
            
            if (Config.StartAudio.ShouldPlay)
                Source.PlayOneShot(Config.StartAudio.Clip, VolumeFactorOverride * Config.StartAudio.VolumeFactor);

            // Can modify the volume when playing, through the config, and by manipulating the Playback instance.
            UpdateAudioSourceVolume();
            Source.clip = Config.LoopingAudioClip;
            Source.loop = true;
            Source.Play();

            // Find the events associated with the clip that we decided to play, and add them to the list of events.
            if (Config.LoopingAudioClip?.Events != null)
                eventsToFire.AddRange(Config.LoopingAudioClip.Events);
            
            // TODO: Also support events for the loop's Start and Stop audio.
        }

        public override void Update()
        {
            base.Update();
            
            // Keep updating the volume and the sound's position until we're told to stop.
            float duration = Config.LoopingAudioClip.AudioClip.length;
            int sampleLength = Source.clip.samples;
            int numberOfSamples = Source.timeSamples;
            
            time = (float)numberOfSamples / sampleLength * duration;

            // If the current time is smaller than the previous time, that means we've looped around. Make sure
            // that we also handle the last segment.
            if (time < timePrevious)
            {
                // First handle any events from where we were, to where the current time would correspond 'beyond'
                // the loop. This is necessary to catch events that are all the way at the end of the track.
                TryFiringRemainingEvents(timePrevious, duration);

                timePrevious -= duration;
            }
                
            TryFiringRemainingEvents(timePrevious, time);
                
            // Make sure the sound comes from the specified transform.
            if (IsLocal)
            {
                if (Origin != null)
                {
                    Source.transform.position = Origin.position;
                }
                else
                {
                    // Audio loop was started on an object but the object was destroyed. Clean up the audio loop
                    // too to prevent it from sticking around.
                    Cleanup();
                }
            }

            UpdateAudioSourceVolume();

            timePrevious = time;
        }

        public UnityAudioLoopingPlayback SetVolumeFactor(float volumeFactor)
        {
            VolumeFactor = volumeFactor;
            return this;
        }

        protected override void OnStop()
        {
            // Squeeze in some cheeky End audio, if specified.
            // TODO: Do we need to wait for this finish before returning the audio source into the pool?
            if (Config.EndAudio.ShouldPlay)
                Source.PlayOneShot(Config.EndAudio.Clip, VolumeFactorOverride * Config.EndAudio.VolumeFactor);
        }

        protected override void OnCleanup()
        {
        }
        
        public override string ToString()
        {
            return Config.name;
        }
    }
}

#endif // UNITY_AUDIO_SYNTAX
