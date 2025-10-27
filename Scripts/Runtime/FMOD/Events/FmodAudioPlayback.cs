#if FMOD_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Debug = UnityEngine.Debug;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Playback for a playable FMOD audio event. Allows you to update its parameters.
    /// Produced by calling Play() on an AudioFmodConfig, which are specified in FmodEvents.AudioEvents
    /// </summary>
    public abstract class FmodAudioPlayback : FmodPlayablePlaybackBase, IFmodAudioPlayback
    {
        private bool isOneshot = false;
        public bool IsOneshot
        {
            get => isOneshot;
            protected set => isOneshot = value;
        }
        
        public delegate void TimelineMarkerReachedHandler(FmodAudioPlayback playback, string markerName);
        private int timelineMarkerListenerCount;
        private event TimelineMarkerReachedHandler timelineMarkerReachedEvent;
        public event TimelineMarkerReachedHandler TimelineMarkerReachedEvent
        {
            add
            {
                timelineMarkerReachedEvent += value;
                timelineMarkerListenerCount++;

                UpdateTimelineMarkerCallbackState();
            }
            remove
            {
                timelineMarkerReachedEvent -= value;
                timelineMarkerListenerCount--;
                
                UpdateTimelineMarkerCallbackState();
            }
        }

        [NonSerialized] private bool hasRegisteredTimelineMarkerReachedCallback;

        [NonSerialized] private bool hasRegisteredPlayback;
        [NonSerialized] private Transform source;

        private Dictionary<string, IAudioPlayback.AudioClipGenericEventHandler> timelineEventIdToHandlers;
        
        public void Play(EventDescription eventDescription, Transform source)
        {
            eventDescription.getPath(out string path);
            
            if (!eventDescription.isValid())
            {
                eventDescription.getID(out GUID guid);
                Debug.LogError($"Trying to play invalid FMOD Event guid: '{guid}' path:'{path}'");
                return;
            }

            this.source = source;

            // Events are called something like event:/ but we want to get rid of any prefix like that.
            // Also every 'folder' along the way will be treated like a sort of 'tag'
            SearchKeywords = path.Substring(path.IndexOf("/", StringComparison.Ordinal) + 1).Replace('/', ',');
            
            Name = System.IO.Path.GetFileName(path);

            EventDescription = eventDescription;
            
            eventDescription.isOneshot(out bool isOneshotResult);
            IsOneshot = isOneshotResult;

            CreateInstance();
        }

        private void CreateInstance()
        {
            EventDescription.createInstance(out EventInstance newInstance);
            Instance = newInstance;
            
            if (source != null)
            {
                Instance.set3DAttributes(RuntimeUtils.To3DAttributes(source));
                RuntimeManager.AttachInstanceToGameObject(Instance, source);
            }

            InitializeParameters();

            Instance.start();

            if (!hasRegisteredPlayback)
            {
                hasRegisteredPlayback = true;
                AudioSyntaxSystem.RegisterActiveEventPlayback(this);
            }
        }

        public void Stop()
        {
            if (Instance.isValid())
                Instance.stop(STOP_MODE.ALLOWFADEOUT);
        }

        public override void Cleanup()
        {
            timelineMarkerReachedEvent = null;
            timelineMarkerListenerCount = 0;
            
            if (Instance.isValid())
            {
                Instance.setCallback(null, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
                
                Instance.stop(STOP_MODE.IMMEDIATE);
                
                RuntimeManager.DetachInstanceFromGameObject(Instance);
                if (EventDescription.isValid())
                {
                    Instance.release();
                    Instance.clearHandle();
                }
            }

            if (hasRegisteredPlayback)
            {
                hasRegisteredPlayback = false;
                AudioSyntaxSystem.UnregisterActiveEventPlayback(this);
            }
        }

        public void Restart()
        {
            Stop();
            
            CreateInstance();
        }
        
        private void UpdateTimelineMarkerCallbackState()
        {
            bool shouldRegisterTimelineMarkerReachedCallback = timelineMarkerListenerCount > 0 ||
                                            (timelineEventIdToHandlers != null && timelineEventIdToHandlers.Count > 0);
            
            if (!hasRegisteredTimelineMarkerReachedCallback && shouldRegisterTimelineMarkerReachedCallback)
                Instance.setCallback(OnTimelineMarkerReached, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
            else if (hasRegisteredTimelineMarkerReachedCallback && !hasRegisteredTimelineMarkerReachedCallback)
                Instance.setCallback(null, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);

            hasRegisteredTimelineMarkerReachedCallback = shouldRegisterTimelineMarkerReachedCallback;
        }

        /// <summary>
        /// Fluid method for subscribing to timeline events so you don't have to save the playback to a variable first
        /// if you don't want to (for example for one-off sounds).
        /// NOTE: Consider using AddTimelineEventHandler which lets you specify which event you are interested in,
        /// and also it is compatible across both the Unity and FMOD audio systems.
        /// </summary>
        public FmodAudioPlayback SubscribeToTimelineMarkerReachedEvent(TimelineMarkerReachedHandler handler)
        {
            TimelineMarkerReachedEvent += handler;
            return this;
        }
        
        /// <summary>
        /// Fluid method for unsubscribing from timeline events so you don't have to save the playback to a variable
        /// first if you don't want to (for example for one-off sounds).
        /// NOTE: Consider using AddTimelineEventHandler which lets you specify which event you are interested in,
        /// and also it is compatible across both the Unity and FMOD audio systems.
        /// </summary>
        public FmodAudioPlayback UnsubscribeFromTimelineMarkerReachedEvent(TimelineMarkerReachedHandler handler)
        {
            TimelineMarkerReachedEvent -= handler;
            return this;
        }
        
        private RESULT OnTimelineMarkerReached(EVENT_CALLBACK_TYPE type, IntPtr @event, IntPtr parameterPtr)
        {
            TIMELINE_MARKER_PROPERTIES parameter = (TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(
                parameterPtr, typeof(TIMELINE_MARKER_PROPERTIES));

            string id = parameter.name;
            
            timelineMarkerReachedEvent?.Invoke(this, id);

            // If a generic event ID handler was registered for this type, then fire those now. 
            if (timelineEventIdToHandlers != null && timelineEventIdToHandlers.TryGetValue(
                    id, out IAudioPlayback.AudioClipGenericEventHandler handlers))
            {
                handlers?.Invoke(this, id);
            }
            
            return RESULT.OK;
        }

        public IAudioPlayback AddTimelineEventHandler(
            AudioTimelineEventId @event, IAudioPlayback.AudioClipGenericEventHandler handler)
        {
            if (timelineEventIdToHandlers == null)
                timelineEventIdToHandlers = new Dictionary<string, IAudioPlayback.AudioClipGenericEventHandler>();

            string id = @event.Id;
            bool existed = timelineEventIdToHandlers.ContainsKey(id);
            if (!existed)
                timelineEventIdToHandlers[id] = handler;
            else
                timelineEventIdToHandlers[id] += handler;
            
            UpdateTimelineMarkerCallbackState();
            
            return this;
        }

        public IAudioPlayback RemoveTimelineEventHandler(
            AudioTimelineEventId @event, IAudioPlayback.AudioClipGenericEventHandler handler)
        {
            string id = @event.Id;

            bool existed = timelineEventIdToHandlers.ContainsKey(id);
            if (existed)
            {
                timelineEventIdToHandlers[id] -= handler;
                if (timelineEventIdToHandlers[id] == null)
                    timelineEventIdToHandlers.Remove(id);
            }

            UpdateTimelineMarkerCallbackState();

            return this;
        }
    }
}

#endif // FMOD_AUDIO_SYNTAX
