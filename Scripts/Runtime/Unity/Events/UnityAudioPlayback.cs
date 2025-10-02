#if UNITY_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Represents a sound that is being played. Returned when the audio system is told to play an audio config.
    /// </summary>
    public abstract class UnityAudioPlayback : IAudioPlayback
    {
        public enum FadeVolumeTypes
        {
            Linear,
            SmoothDamp,
        }

        public UnityAudioEventConfigAssetBase BaseEventConfig;
        
        private IList<UnityAudioTag> Tags => BaseEventConfig.Tags;

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
        public bool CanBeCleanedUp => canBeCleanedUp || markedAsInvalid;

        public string Name => BaseEventConfig.name;

        [NonSerialized] private bool didCacheSearchKeywords;
        [NonSerialized] private string searchKeywords;
        
        /// <summary>
        /// Useful for things like quickly filtering out a subgroup of active events while debugging.
        /// </summary>
        public string SearchKeywords
        {
            get
            {
                if (!didCacheSearchKeywords)
                {
                    didCacheSearchKeywords = true;
                    
                    searchKeywords += Name;
                    
                    // Add a keyword for every tag.
                    for (int i = 0; i < Tags.Count; i++)
                    {
                        searchKeywords += ",";
                        
                        searchKeywords += Tags[i].name;
                    }
                }
                return searchKeywords;
            }
        }

        public abstract bool IsOneshot
        {
            get;
        }

        private bool isLoadingAsynchronously;
        public bool IsLoadingAsynchronously => isLoadingAsynchronously;

        protected float time;
        protected float timePrevious;

        protected float normalizedProgress;
        public float NormalizedProgress => normalizedProgress;
        
#if UNITY_EDITOR
        private double lastEditorUpdateTime;
#endif // UNITY_EDITOR
        
        private float deltaTime;
        protected float DeltaTime => deltaTime;

        private Dictionary<string, IAudioPlayback.AudioClipGenericEventHandler> genericEventIdToHandlers;

        private bool markedAsInvalid;
        
        private bool isTweeningVolume;
        private FadeVolumeTypes volumeTweenType;
        private float volumeTweenTarget;
        private float volumeTweenDuration;
        private float volumeTweenVelocity;
        private bool isFadingOut;

        private void InitializeInternal(Transform origin, float volumeFactorOverride)
        {
            this.volumeFactorOverride = volumeFactorOverride;
            
            source = UnityAudioSyntaxSystem.Instance.GetAudioSourceForPlayback();
            
            // Need to do this after a source is assigned because this also updates the source's spatial blend.
            Origin = origin;
        }

        private void Initialize(UnityAudioEventConfigAssetBase audioEventConfig,
            Transform origin, float volumeFactorOverride)
        {
            InitializeInternal(origin, volumeFactorOverride);
            
            CompleteInitialization(audioEventConfig);
        }

        private void InitializeAsLoadingAsynchronously(Transform origin, float volumeFactorOverride)
        {
            InitializeInternal(origin, volumeFactorOverride);
            
            // Just mark the playback as being in the process of using asynchronous loading to load the config.
            // When the config is loaded, the playback is notified and its initialization will be completed.
            isLoadingAsynchronously = true;
        }
        
        public void CompleteInitialization(UnityAudioEventConfigAssetBase config)
        {
            BaseEventConfig = config;
            
            // Assign the mixer group now that we know which config to use.
            if (config.MixerGroup == null)
                source.outputAudioMixerGroup = UnityAudioSyntaxSettings.Instance.DefaultMixerGroup;
            else
                source.outputAudioMixerGroup = config.MixerGroup;

            if (isLoadingAsynchronously)
                isLoadingAsynchronously = false;

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
            
#if UNITY_EDITOR
            lastEditorUpdateTime = EditorApplication.timeSinceStartup;
#endif

            OnStart();
        }

        protected abstract void OnStart();
        
        public virtual void Update()
        {
            deltaTime = Time.deltaTime;
            
#if UNITY_EDITOR
            // Keep track of Delta Time in a way that works in the editor.
            // This is necessary for being able to play back audio previews.
            if (!Application.isPlaying)
            {
                deltaTime = (float)(EditorApplication.timeSinceStartup - lastEditorUpdateTime);
                lastEditorUpdateTime = EditorApplication.timeSinceStartup;
            }
#endif

            UpdateVolumeTween();
        }
        
        private void UpdateVolumeTween()
        {
            // Tween towards a specified value.
            if (!isTweeningVolume)
                return;
            
            // We support doing that linearly or with smooth damping.
            switch (volumeTweenType)
            {
                case FadeVolumeTypes.Linear:
                    Volume = Mathf.MoveTowards(Volume, volumeTweenTarget, Time.deltaTime / volumeTweenDuration);
                    break;
                    
                case FadeVolumeTypes.SmoothDamp:
                    Volume = Mathf.SmoothDamp(Volume, volumeTweenTarget, ref volumeTweenVelocity, volumeTweenDuration);
                    break;
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // If we've reached our target then we may want to stop the event completely, if requested.
            if (Mathf.Approximately(Volume, volumeTweenTarget))
            {
                Volume = volumeTweenTarget;
                isTweeningVolume = false;

                if (isFadingOut && Volume.Approximately(0.0f))
                    FullyFadedOut();
            }
        }

        private void FullyFadedOut()
        {
            Stop();
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
        
        protected void MarkAsInvalid()
        {
            markedAsInvalid = true;
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

        public IAudioPlayback AddTimelineEventHandler(
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
        
        public IAudioPlayback RemoveTimelineEventHandler(
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
        
        private static void InitializePlayback<PlaybackType>(PlaybackType playback, 
            UnityAudioEventConfigAssetBase audioEventConfig, Transform origin, float volumeFactor)
            where PlaybackType : UnityAudioPlayback, new()
        {
            playback.Initialize(audioEventConfig, origin, volumeFactor);
            
#if DEBUG_AUDIO_SOURCE_POOLING && UNITY_EDITOR
            audioSource.name = "AudioSource - " + playback;
#endif // DEBUG_AUDIO_SOURCE_POOLING

            AudioSyntaxSystem.RegisterActiveEventPlayback(playback);
        }

        public static PlaybackType Play<PlaybackType>(
            UnityAudioEventConfigAssetBase audioEventConfig, Transform origin, float volumeFactor = 1.0f)
            where PlaybackType : UnityAudioPlayback, new()
        {
            PlaybackType playback = new();
            
            InitializePlayback(playback, audioEventConfig, origin, volumeFactor);

            return playback;
        }

        public static PlaybackType PlayWithAsynchronousLoading<PlaybackType>(
            Transform origin, float volumeFactor = 1.0f)
            where PlaybackType : UnityAudioPlayback, new()
        {
            PlaybackType playback = new();
            
            // Config is not available yet and will finish loading later. Initialize a playback instance as best we can,
            // its initialization will be completed once the config has finished loading.
            playback.InitializeAsLoadingAsynchronously(origin, volumeFactor);

            return playback;
        }
        
        protected void ReportInvalidAudioClip(string name, bool markAsInvalid)
        {
            if (markAsInvalid)
                MarkAsInvalid();
            
            Debug.LogError($"Audio Event config '<b>{BaseEventConfig.Path}</b>' did not have a valid {name} " +
                           $"audio clip...", BaseEventConfig);
        }
        
        public void SetFadeVolumeTypeGeneric(FadeVolumeTypes fadeVolumeType)
        {
            volumeTweenType = fadeVolumeType;
        }

        private void FadeVolumeToInternalGeneric(float value, float duration)
        {
            if (Volume.Approximately(value))
                return;
            
            isTweeningVolume = true;
            volumeTweenDuration = duration;
            volumeTweenTarget = value;
        }
        
        public void FadeVolumeToGeneric(float value, float duration)
        {
            isFadingOut = false;
            
            FadeVolumeToInternalGeneric(value, duration);
        }
        
        public void FadeInGeneric(float duration, bool startAtZeroVolume = true)
        {
            isFadingOut = false;
            
            if (startAtZeroVolume)
                Volume = 0.0f;
            
            FadeVolumeToInternalGeneric(1.0f, duration);
        }
        
        public void FadeOutGeneric(float duration, bool stopWhenFullyFadedOut = true)
        {
            isFadingOut = stopWhenFullyFadedOut;

            if (stopWhenFullyFadedOut && Volume.Approximately(0.0f) && !CanBeCleanedUp)
            {
                FullyFadedOut();
                return;
            }
            
            FadeVolumeToInternalGeneric(0.0f, duration);
        }

#if UNITY_EDITOR
        private List<KeyValuePair<string, object>> debugInformation;
        
        protected void AddDebugInformation(string label, object value)
        {
            debugInformation.Add(new KeyValuePair<string, object>(label, value));
        }
        
        protected void AddDebugInformation(string label, LastPlayedAudioData lastPlayedAudioData)
        {
            AddDebugInformation(label, "HEADER");

            const string LabelPrefix = "Last Played ";
            const string LabelClip = LabelPrefix + "Clip";
            const string LabelVolume = LabelPrefix + "Volume";
            const string LabelPitch = LabelPrefix + "Pitch";
            const string ValueInvalid = "-";

            if (!lastPlayedAudioData.HasPlayed)
            {
                AddDebugInformation(LabelClip, ValueInvalid);
                AddDebugInformation(LabelVolume, ValueInvalid);
                AddDebugInformation(LabelPitch, ValueInvalid);
                return;
            }
            
            AddDebugInformation(LabelClip, lastPlayedAudioData.AudioClip);
            AddDebugInformation(LabelVolume, lastPlayedAudioData.Volume);
            AddDebugInformation(LabelPitch, lastPlayedAudioData.Pitch);
        }

        public void GetDebugInformation(List<KeyValuePair<string, object>> debugInformation)
        {
            this.debugInformation = debugInformation;
            
            GetDebugInformationInternal();
        }

        protected abstract void GetDebugInformationInternal();
#endif // UNITY_EDITOR
    }
    
    public abstract class UnityAudioPlaybackGeneric<AudioConfigType, ThisType> : UnityAudioPlayback
        where AudioConfigType : UnityAudioEventConfigAssetBase
        where ThisType : UnityAudioPlayback, IAudioPlayback
    {
        protected AudioConfigType Config => (AudioConfigType)BaseEventConfig;
        
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

        protected abstract bool ShouldFireEventsOnlyOnce { get; }
        
        protected readonly List<AudioClipTimelineEvent> timelineEventsToFire = new(0);
        private Dictionary<AudioTimelineEventId, AudioClipEventHandler> timelineEventIdToHandlers;

        public delegate void AudioClipEventHandler(ThisType audioPlayback, AudioTimelineEventId eventId);

        public override void Update()
        {
            base.Update();
            
            UpdateAudioSourceVolume();
        }

        protected override void OnCleanupInternal()
        {
            timelineEventIdToHandlers?.Clear();
        }
        
        protected void UpdateAudioSourceVolume()
        {
            Source.volume = VolumeFactorOverride * Config.VolumeFactor.Evaluate(this) * Volume;
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

        public ThisType SetFadeVolumeType(FadeVolumeTypes fadeVolumeType)
        {
            SetFadeVolumeTypeGeneric(fadeVolumeType);
            return this as ThisType;
        }
        
        public ThisType FadeVolumeTo(float value, float duration)
        {
            FadeVolumeToGeneric(value, duration);
            return this as ThisType;
        }
        
        public ThisType FadeIn(float duration, bool startAtZeroVolume = true)
        {
            FadeInGeneric(duration, startAtZeroVolume);
            return this as ThisType;
        }
        
        public ThisType FadeOut(float duration, bool stopWhenFullyFadedOut = true)
        {
            FadeOutGeneric(duration, stopWhenFullyFadedOut);
            return this as ThisType;
        }
    }
}

#endif // UNITY_AUDIO_SYNTAX
