#if UNITY_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using RoyTheunissen.FMODSyntax.TimelineEvents;
using UnityEngine;
using UnityEngine.Audio;

namespace RoyTheunissen.FMODSyntax.UnityAudioSyntax
{
    /// <summary>
    /// Represents a sound that is being played. Returned when the audio system is told to play an audio config.
    /// </summary>
    public abstract class UnityAudioPlayback : IAudioPlayback
    {
        protected UnityAudioConfigBase baseConfig;

        // TODO: Support tags again
        // private CollectionItemPicker<AudioTag> Tags => baseConfig.Tags;

        private float volumeFactorOverride;
        public float VolumeFactorOverride => volumeFactorOverride;

        private AudioSource source;
        protected AudioSource Source => source;

        private bool isPlaying;
        public bool IsPlaying => isPlaying;

        private bool didCleanUp;
        public bool DidCleanUp => didCleanUp;
        
        private bool isLocal;
        public bool IsLocal => isLocal;

        private Transform cachedOrigin;
        public Transform Origin
        {
            get => cachedOrigin;
            set
            {
                cachedOrigin = value;
                isLocal = value != null;
                UpdateSpatialBlend();
            }
        }

        public abstract float Volume { get; set; }

        private bool canBeCleanedUp;
        public bool CanBeCleanedUp => canBeCleanedUp;

        public string Name => baseConfig.name;

        [NonSerialized] private bool didCacheSearchKeywords;
        [NonSerialized] private string searchKeywords;
        public string SearchKeywords
        {
            get
            {
                if (!didCacheSearchKeywords)
                {
                    didCacheSearchKeywords = true;
                    
                    // TODO: Support tags again
                    // Add a keyword for every tag.
                    // for (int i = 0; i < Tags.Count; i++)
                    // {
                    //     searchKeywords += Tags[i].name;
                    //     if (i < Tags.Count - 1)
                    //         searchKeywords += ",";
                    // }
                    
                    // Also add the name itself.
                    // if (Tags.Count > 0)
                    //     searchKeywords += ",";
                    searchKeywords += Name;
                }
                return searchKeywords;
            }
        }

        public abstract bool IsOneshot
        {
            get;
        }

        protected float time;
        protected float timePrevious;

        protected float normalizedProgress;
        public float NormalizedProgress => normalizedProgress;
        
        private Dictionary<string, IAudioPlayback.AudioClipGenericEventHandler> genericEventIdToHandlers;

        public void Initialize(
            UnityAudioConfigBase audioConfig, Transform origin, float volumeFactorOverride, AudioSource audioSource)
        {
            baseConfig = audioConfig;
            this.volumeFactorOverride = volumeFactorOverride;
            source = audioSource;
            
            // Need to do this after a source is assigned because this also updates the source's spatial blend.
            Origin = origin;

            Start();
        }

        private void UpdateSpatialBlend()
        {
            Source.spatialBlend = isLocal ? 1 : 0;
        }

        private void Start()
        {
            if (isPlaying)
                return;
            
            isPlaying = true;

            OnStart();
        }

        protected abstract void OnStart();
        
        public virtual void Update()
        {
            
        }

        /// <summary>
        /// Stop is used to indicate that the audio is finished and should commence the stopping behaviour, which may
        /// include playing stopping sounds, and eventually calls Cleanup as well. If you want to clean up a sound
        /// without triggering any additional sounds consider calling Cleanup directly.
        /// </summary>
        public void Stop()
        {
            if (!isPlaying)
                return;
            
            isPlaying = false;
            
            OnStop();
        }
        
        protected abstract void OnStop();

        protected void MarkForCleanup()
        {
            canBeCleanedUp = true;
        }

        /// <summary>
        /// Cleanup is responsible for finalizing playback and de-allocating whatever resources were used. 
        /// </summary>
        public void Cleanup()
        {
            if (didCleanUp)
                return;
            didCleanUp = true;
            
            isPlaying = false;
            
            if (Source != null)
            {
                Source.Stop();

                UnityAudioSyntaxSystem.Instance.ReturnAudioSourceForPlayback(Source);
                AudioSyntaxSystem.UnregisterActiveEventPlayback(this);
            }

            OnCleanupInternal();
            OnCleanup();
        }

        protected abstract void OnCleanupInternal();

        protected abstract void OnCleanup();

        IAudioPlayback IAudioPlayback.AddTimelineEventHandler(
            AudioTimelineEventId @event, IAudioPlayback.AudioClipGenericEventHandler handler)
        {
            if (genericEventIdToHandlers == null)
                genericEventIdToHandlers = new Dictionary<string, IAudioPlayback.AudioClipGenericEventHandler>();

            string id = @event.Id;
            bool existed = genericEventIdToHandlers.ContainsKey(id);
            if (!existed)
                genericEventIdToHandlers[id] = handler;
            else
                genericEventIdToHandlers[id] += handler;
            
            return this;
        }
        
        IAudioPlayback IAudioPlayback.RemoveTimelineEventHandler(
            AudioTimelineEventId @event, IAudioPlayback.AudioClipGenericEventHandler handler)
        {
            string id = @event.Id;

            bool existed = genericEventIdToHandlers.ContainsKey(id);
            if (existed)
            {
                genericEventIdToHandlers[id] -= handler;
                if (genericEventIdToHandlers[id] == null)
                    genericEventIdToHandlers.Remove(id);
            }

            return this;
        }
        
        protected virtual void FireTimelineEvent(AudioTimelineEventId @event)
        {
            if (genericEventIdToHandlers == null)
                return;

            bool existed = genericEventIdToHandlers.TryGetValue(
                @event.Id, out IAudioPlayback.AudioClipGenericEventHandler handler);
            if (existed)
                handler(this, @event.Id);
        }
    }
    
    public abstract class UnityAudioPlaybackGeneric<AudioConfigType, ThisType> : UnityAudioPlayback
        where AudioConfigType : UnityAudioConfigBase
        where ThisType : UnityAudioPlayback, IAudioPlayback
    {
        protected AudioConfigType Config => (AudioConfigType)baseConfig;
        
        private float volumeFactor = 1.0f;
        
        private float volumeAdjustVelocity;
        
        public override float Volume
        {
            get => volumeFactor;
            set
            {
                volumeFactor = value;
                UpdateAudioSourceVolume();
            }
        }

        public float Pitch
        {
            get => Source.pitch;
            set => Source.pitch = value;
        }
        
        // TODO: Add support for tweens back?
        // private Tween cachedVolumeTween;
        // public Tween VolumeTween
        // {
        //     get
        //     {
        //         if (cachedVolumeTween == null)
        //             cachedVolumeTween = new Tween(f => VolumeFactor = f).SkipTo(VolumeFactor);
        //         return cachedVolumeTween;
        //     }
        // }

        protected abstract bool ShouldFireEventsOnlyOnce { get; }
        
        protected readonly List<AudioClipTimelineEvent> timelineEventsToFire = new(0);
        private Dictionary<AudioTimelineEventId, AudioClipEventHandler> timelineEventIdToHandlers;
        
        public delegate void AudioClipEventHandler(ThisType audioPlayback, AudioTimelineEventId eventId);

        protected override void OnCleanupInternal()
        {
            timelineEventIdToHandlers?.Clear();
            
            // TODO: Add support for tweens back?
            //cachedVolumeTween.Cleanup();
        }
        
        protected void UpdateAudioSourceVolume()
        {
            Source.volume = VolumeFactorOverride * Config.VolumeFactor * Volume;
        }
        
        protected void TryFiringRemainingEvents(
            List<AudioClipTimelineEvent> timelineEventsToFire, float timePrevious, float timeCurrent)
        {
            if (timelineEventsToFire == null)
                return;
        
            for (int i = timelineEventsToFire.Count - 1; i >= 0; i--)
            {
                if (!(timePrevious.EqualOrSmaller(timelineEventsToFire[i].Time) &&
                      timeCurrent.EqualOrGreater(timelineEventsToFire[i].Time) && !timePrevious.Equal(timeCurrent)))
                {
                    continue;
                }
                
                FireTimelineEvent(timelineEventsToFire[i].Id);
                    
                if (ShouldFireEventsOnlyOnce)
                    timelineEventsToFire.RemoveAt(i);
            }
        }
        
        protected void TryFiringRemainingEvents(float timePrevious, float timeCurrent)
        {
            TryFiringRemainingEvents(timelineEventsToFire, timePrevious, timeCurrent);
        }

        // NOTE: Fluid initialization methods can be done on the generic level like this, having it still return the
        // actual type of the playback class itself.
        public ThisType SetOrigin(Transform origin)
        {
            Origin = origin;

            return this as ThisType;
        }
        
        public ThisType MoveTowardsVolume(float volume, float amount)
        {
            Volume = Mathf.MoveTowards(Volume, volume, amount);
            return this as ThisType;
        }
        
        public ThisType SmoothDampVolume(float volume, float duration)
        {
            Volume = Mathf.SmoothDamp(Volume, volume, ref volumeAdjustVelocity, duration);
            return this as ThisType;
        }
        
        public ThisType SetBypassAllEffects(bool bypassAllEffects)
        {
            Source.bypassEffects = bypassAllEffects;
            Source.bypassListenerEffects = bypassAllEffects;
            Source.bypassReverbZones = bypassAllEffects;
            return this as ThisType;
        }
        
        public ThisType SetBypassEffects(bool bypassEffects)
        {
            Source.bypassEffects = bypassEffects;
            return this as ThisType;
        }
        
        public ThisType SetBypassListenerEffects(bool bypassListenerEffects)
        {
            Source.bypassListenerEffects = bypassListenerEffects;
            return this as ThisType;
        }
        
        public ThisType SetBypassReverbZones(bool bypassReverbZones)
        {
            Source.bypassReverbZones = bypassReverbZones;
            return this as ThisType;
        }
        
        public ThisType SetStereoPan(float stereoPan)
        {
            Source.panStereo = stereoPan;
            return this as ThisType;
        }
        
        public ThisType SetSpatialBlend(float spatialBlend)
        {
            Source.spatialBlend = spatialBlend;
            return this as ThisType;
        }

        public ThisType SetRolloff(
            float maxRange, AudioRolloffMode mode = AudioRolloffMode.Logarithmic, float minRange = 1.0f,
            AnimationCurve curve = null)
        {
            Source.maxDistance = maxRange;
            Source.rolloffMode = mode;
            Source.minDistance = minRange;

            if (mode == AudioRolloffMode.Custom)
                Source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);

            return this as ThisType;
        }
        
        public ThisType SetDopplerLevel(float dopplerLevel)
        {
            Source.dopplerLevel = dopplerLevel;

            return this as ThisType;
        }
                
        public ThisType SetMixerGroup(AudioMixerGroup mixerGroup)
        {
            Source.outputAudioMixerGroup = mixerGroup;
            return this as ThisType;
        }
        
        public ThisType AddTimelineEventHandler(AudioTimelineEventId @event, AudioClipEventHandler handler)
        {
            if (timelineEventIdToHandlers == null)
                timelineEventIdToHandlers = new Dictionary<AudioTimelineEventId, AudioClipEventHandler>();
            
            bool existed = timelineEventIdToHandlers.ContainsKey(@event);
            if (!existed)
                timelineEventIdToHandlers[@event] = handler;
            else
                timelineEventIdToHandlers[@event] += handler;
            return this as ThisType;
        }
        
        public ThisType ClearAllTimelineEventHandlers()
        {
            timelineEventsToFire?.Clear();
            timelineEventIdToHandlers?.Clear();
        
            return this as ThisType;
        }

        protected override void FireTimelineEvent(AudioTimelineEventId @event)
        {
            base.FireTimelineEvent(@event);
            
            // The base class supports events that use a string identifier, that's the solution that works for both
            // FMOD and Unity. We in turn support a version that uses AudioClipEventId Scriptable Objects as the ID.
            // That's a bit nicer to work with.
            if (timelineEventIdToHandlers == null)
                return;
            
            bool existed = timelineEventIdToHandlers.TryGetValue(@event, out AudioClipEventHandler handler);
            if (existed)
                handler(this as ThisType, @event);
        }
    }
}

#endif // UNITY_AUDIO_SYNTAX
