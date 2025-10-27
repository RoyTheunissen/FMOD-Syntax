#if UNITY_AUDIO_SYNTAX

using System.Collections.Generic;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Config asset for a looping audio event as played back by Unity's native audio system.
    /// </summary>
    public class UnityAudioEventLoopingConfigAsset : UnityAudioEventConfigAsset<UnityAudioLoopingPlayback>
    {
        [Space]
        [SerializeField] private AudioLoopBookendConfig startAudio;
        public AudioLoopBookendConfig StartAudio => startAudio;
        
        [Space]
        [SerializeField] private AudioEventConfigPropertyAudioClips loopingAudioClips = new();
        public AudioEventConfigPropertyAudioClips LoopingAudioClips => loopingAudioClips;

        [Space]
        [SerializeField] private AudioLoopBookendConfig endAudio;
        public AudioLoopBookendConfig EndAudio => endAudio;
    }

    /// <summary>
    /// Responsible for the playback of a looping audio event as played back by Unity's native audio system.
    /// </summary>
    public class UnityAudioLoopingPlayback : UnityAudioPlaybackGeneric<UnityAudioEventLoopingConfigAsset, UnityAudioLoopingPlayback>
    {
        protected override bool ShouldFireEventsOnlyOnce => false;
        
        private bool waitForEndSoundToFinish;

        public override bool IsOneshot => false;

        private bool hasStartAudioTimelineEvents;
        private List<AudioClipTimelineEvent> startAudioTimelineEvents;
        private float startAudioTime;
        
        private bool hasEndAudioTimelineEvents;
        private List<AudioClipTimelineEvent> endAudioTimelineEvents;
        private float endAudioTime;
        private AudioClipMetaData endAudioClip;
        
#if UNITY_EDITOR
        private LastPlayedAudioData lastPlayedStartAudioData = LastPlayedAudioData.Default;
        private LastPlayedAudioData lastPlayedLoopingAudioData = LastPlayedAudioData.Default;
        private LastPlayedAudioData lastPlayedStopAudioData = LastPlayedAudioData.Default;
#endif // UNITY_EDITOR

        protected override void OnStart()
        {
            waitForEndSoundToFinish = false;

            startAudioTime = 0.0f;
            endAudioTime = 0.0f;
            
            if (Config.StartAudio.ShouldPlay)
                PlayStartAudio();

            // Can modify the volume when playing, through the config, and by manipulating the Playback instance.
            UpdateAudioSourceVolume();
            AudioClipMetaData loopingAudioClip = Config.LoopingAudioClips.GetAudioClipToPlay(this);
            
            if (loopingAudioClip == null || loopingAudioClip.AudioClip == null)
            {
                MarkAsInvalid();
                ReportInvalidAudioClip("Looping", true);
                return;
            }
            
            Source.clip = loopingAudioClip;
            Source.loop = true;
            Source.Play();
            
#if UNITY_EDITOR
            lastPlayedLoopingAudioData = new LastPlayedAudioData(Source);
#endif // UNITY_EDITOR

            // Find the events associated with the clip that we decided to play, and add them to the list of events.
            if (loopingAudioClip?.TimelineEvents != null)
                timelineEventsToFire.AddRange(loopingAudioClip.TimelineEvents);
        }

        private void PlayStartAudio()
        {
            AudioClipMetaData startAudioClip = Config.StartAudio.AudioClips.GetAudioClipToPlay(this);
            if (startAudioClip == null || startAudioClip.AudioClip == null)
            {
                ReportInvalidAudioClip("Start", false);
                return;
            }

            float startAudioVolume = VolumeFactorOverride * Config.StartAudio.VolumeFactor.Evaluate(this);
            // TODO: Support randomized pitch for start audio?
            float startAudioPitch = 1.0f;
            Source.PlayOneShot(startAudioClip, startAudioVolume);
                
#if UNITY_EDITOR
            lastPlayedStartAudioData = new LastPlayedAudioData(startAudioClip, startAudioVolume, startAudioPitch);
#endif // UNITY_EDITOR
                
            // You're allowed to specify timeline events for the start audio clip too. 
            if (startAudioClip?.TimelineEvents != null && startAudioClip?.TimelineEvents.Count > 0)
            {
                hasStartAudioTimelineEvents = true;
                if (startAudioTimelineEvents == null)
                    startAudioTimelineEvents = new List<AudioClipTimelineEvent>();
                else
                    startAudioTimelineEvents.Clear();
                startAudioTimelineEvents.AddRange(startAudioClip?.TimelineEvents);
            }
        }

        public override void Update()
        {
            base.Update();
            
            // Keep updating the volume and the sound's position until we're told to stop.

            UpdateTimeAndFireTimelineEvents();

            timePrevious = time;
        }

        private void UpdateTimeAndFireTimelineEvents()
        {
            // Update the time and fire timeline events for the looping audio clip.
            if (!waitForEndSoundToFinish)
            {
                AudioClip loopingClip = Source.clip;
                float duration = loopingClip.length;
                int sampleLength = loopingClip.samples;
                int numberOfSamples = Source.timeSamples;
            
                time = (float)numberOfSamples / sampleLength * duration;
                normalizedProgress = (time / duration).Saturate();

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
            }
            
            // Also perform timeline events for the start audio, which we need to keep track of separately because the
            // duration may be greater than that of one loop, and the time needs to progress all the way to the end.
            if (Config.StartAudio.ShouldPlay)
            {
                // NOTE: Can't use the super accurate Source.timeSamples approach because there is no dedicated
                // Audio Source to get this value from. Just use DeltaTime, it's probably good enough.
                float startAudioTimePrevious = startAudioTime;
                startAudioTime += DeltaTime;
                
                if (hasStartAudioTimelineEvents)
                    TryFiringRemainingEvents(startAudioTimelineEvents, startAudioTimePrevious, startAudioTime);
            }
            
            // Also perform timeline events for the end audio, which we need to keep track of separately because the
            // duration may be greater than that of one loop, and the time needs to progress all the way to the end.
            if (Config.EndAudio.ShouldPlay && waitForEndSoundToFinish)
            {
                // NOTE: Can't use the super accurate Source.timeSamples approach because there is no dedicated
                // Audio Source to get this value from. Just use DeltaTime, it's probably good enough.
                float endAudioTimePrevious = endAudioTime;
                endAudioTime += DeltaTime;
                
                if (hasEndAudioTimelineEvents)
                    TryFiringRemainingEvents(endAudioTimelineEvents, endAudioTimePrevious, endAudioTime);
                
                if (endAudioTime.Greater(endAudioClip.AudioClip.length))
                {
                    waitForEndSoundToFinish = false;
                    MarkForCleanup();
                }
            }
        }

        public UnityAudioLoopingPlayback SetVolume(float volume)
        {
            Volume = volume;
            return this;
        }

        protected override void OnStop()
        {
            // Squeeze in some cheeky End audio, if specified. If so we also need to wait for it to finish before
            // we return our Audio Source to the pool.
            if (Config.EndAudio.ShouldPlay)
            {
                PlayEndAudio();
            }
            else
            {
                waitForEndSoundToFinish = false;
                MarkForCleanup();
            }
        }

        private void PlayEndAudio()
        {
            Source.Stop();
            
            endAudioClip = Config.EndAudio.AudioClips.GetAudioClipToPlay(this);
            
            if (endAudioClip == null || endAudioClip.AudioClip == null)
            {
                ReportInvalidAudioClip("End", false);
                return;
            }
            
            waitForEndSoundToFinish = true;
            
            float endAudioVolume = VolumeFactorOverride * Config.EndAudio.VolumeFactor.Evaluate(this);
            // TODO: Support randomized pitch for end audio?
            float endAudioPitch = 1.0f;
            Source.PlayOneShot(endAudioClip, endAudioVolume);
                
#if UNITY_EDITOR
            lastPlayedStopAudioData = new LastPlayedAudioData(endAudioClip, endAudioVolume, endAudioPitch);
#endif // UNITY_EDITOR
                
            // You're allowed to specify timeline events for the end audio clip too. 
            if (endAudioClip?.TimelineEvents != null && endAudioClip?.TimelineEvents.Count > 0)
            {
                hasEndAudioTimelineEvents = true;
                if (endAudioTimelineEvents == null)
                    endAudioTimelineEvents = new List<AudioClipTimelineEvent>();
                else
                    endAudioTimelineEvents.Clear();
                endAudioTimelineEvents.AddRange(endAudioClip?.TimelineEvents);
            }
        }

        protected override void OnCleanup()
        {
        }
        
        public override string ToString()
        {
            return Config.name;
        }
        
#if UNITY_EDITOR
        protected override void GetDebugInformationInternal()
        {
            AddDebugInformation("Start Audio", lastPlayedStartAudioData);
            
            AddDebugInformation("Looping Audio", lastPlayedLoopingAudioData);
            
            AddDebugInformation("End Audio", lastPlayedStopAudioData);
        }
#endif // UNITY_EDITOR
    }
}

#endif // UNITY_AUDIO_SYNTAX
