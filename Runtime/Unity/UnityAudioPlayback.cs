using System;
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

        public abstract float Volume { get; }

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

        public abstract float NormalizedProgress
        {
            get;
        }

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
            
            // Return the audio source to the pool AFTER the derived class has had a chance to stop. This allows looping
            // sounds to squeeze in a cheeky one-off end sound before the audio source is returned.
            MarkForCleanup();
        }
        
        protected abstract void OnStop();

        private void MarkForCleanup()
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
    }
    
    public abstract class UnityAudioPlaybackGeneric<AudioConfigType, ThisType> : UnityAudioPlayback
        where AudioConfigType : UnityAudioConfigBase
        where ThisType : UnityAudioPlayback, IAudioPlayback
    {
        protected AudioConfigType Config => (AudioConfigType)baseConfig;
        
        private float volumeFactor = 1.0f;
        
        private float volumeAdjustVelocity;
        
        public override float Volume => VolumeFactor;

        public float VolumeFactor
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
        
        // TODO: Add support for events back
        // protected readonly List<AudioClipEvent> eventsToFire = new List<AudioClipEvent>(0);
        // private Dictionary<AudioClipEventId, AudioClipEventHandler> eventIdToHandlers;
        //
        // public delegate void AudioClipEventHandler(ThisType audioPlayback, AudioClipEventId eventId);

        protected override void OnCleanupInternal()
        {
            // TODO: Add support for events back
            //eventIdToHandlers?.Clear();
            
            // TODO: Add support for tweens back?
            //cachedVolumeTween.Cleanup();
        }
        
        protected void UpdateAudioSourceVolume()
        {
            Source.volume = VolumeFactorOverride * Config.VolumeFactor * VolumeFactor;
        }

        // TODO: Add support for events back
        // protected void TryFiringRemainingEvents(float timePrevious, float timeCurrent)
        // {
        //     if (eventsToFire == null)
        //         return;
        //
        //     for (int i = eventsToFire.Count - 1; i >= 0; i--)
        //     {
        //         if (!(timePrevious.EqualOrSmaller(eventsToFire[i].Time) &&
        //               timeCurrent.EqualOrGreater(eventsToFire[i].Time) && !timePrevious.Equal(timeCurrent)))
        //         {
        //             continue;
        //         }
        //         
        //         FireEvent(eventsToFire[i].Id);
        //             
        //         if (ShouldFireEventsOnlyOnce)
        //             eventsToFire.RemoveAt(i);
        //     }
        // }

        // NOTE: Fluid initialization methods can be done on the generic level like this, having it still return the
        // actual type of the playback class itself.
        public ThisType SetOrigin(Transform origin)
        {
            Origin = origin;

            return this as ThisType;
        }
        
        public ThisType MoveTowardsVolume(float volume, float amount)
        {
            VolumeFactor = Mathf.MoveTowards(VolumeFactor, volume, amount);
            return this as ThisType;
        }
        
        public ThisType SmoothDampVolume(float volume, float duration)
        {
            VolumeFactor = Mathf.SmoothDamp(VolumeFactor, volume, ref volumeAdjustVelocity, duration);
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

        // TODO: Add support for events back
        // public ThisType AddEventHandler(AudioClipEventId @event, AudioClipEventHandler handler)
        // {
        //     if (eventIdToHandlers == null)
        //         eventIdToHandlers = new Dictionary<AudioClipEventId, AudioClipEventHandler>();
        //     
        //     bool existed = eventIdToHandlers.ContainsKey(@event);
        //     if (!existed)
        //         eventIdToHandlers[@event] = handler;
        //     else
        //         eventIdToHandlers[@event] += handler;
        //     return this as ThisType;
        // }
        //
        // public ThisType ClearAllEventHandlers()
        // {
        //     eventsToFire?.Clear();
        //     eventIdToHandlers?.Clear();
        //
        //     return this as ThisType;
        // }
        //
        // protected void FireEvent(AudioClipEventId @event)
        // {
        //     if (eventIdToHandlers == null)
        //         return;
        //     
        //     bool existed = eventIdToHandlers.TryGetValue(@event, out AudioClipEventHandler handler);
        //     if (existed)
        //         handler(this as ThisType, @event);
        // }
    }
}
