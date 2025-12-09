#if FMOD_AUDIO_SYNTAX

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        // ------------------------------------------------------------------------------------------------------------
        // Hideous timeline callback functionality
        
        EVENT_CALLBACK timelineEventCallback;
        // ------------------------------------------------------------------------------------------------------------
        
        
        private bool isOneshot = false;
        public bool IsOneshot
        {
            get => isOneshot;
            protected set => isOneshot = value;
        }

        [NonSerialized] private SpatializationTypes spatializationType;
        [NonSerialized] private Transform spatializationTransform;
        [NonSerialized] private Vector3 spatializationStaticPosition;

        private readonly ConcurrentQueue<string> timelineMarkersReached = new();
        
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

        private Dictionary<string, IAudioPlayback.AudioClipGenericEventHandler> timelineEventIdToHandlers;

        public void Update()
        {
            // Timeline marker callbacks happen on a separate FMOD audio thread, so to safely pass it along to Unity,
            // we need to buffer the timeline marker events and fire them on the next update of the main thread.
            while (timelineMarkersReached.TryDequeue(out string id))
            {
                timelineMarkerReachedEvent?.Invoke(this, id);
            
                // If a generic event ID handler was registered for this type, then fire those now. 
                if (timelineEventIdToHandlers != null && timelineEventIdToHandlers.TryGetValue(
                        id, out IAudioPlayback.AudioClipGenericEventHandler handlers))
                {
                    handlers?.Invoke(this, id);
                }
            }
        }
        
        public void Play(EventDescription eventDescription, Transform source)
        {
            spatializationType = source == null ? SpatializationTypes.Global : SpatializationTypes.Transform;
            spatializationTransform = source;
            
            PlayInternal(eventDescription);
        }
        
        public void Play(EventDescription eventDescription, Vector3 staticPosition)
        {
            spatializationType = SpatializationTypes.StaticPosition;
            spatializationStaticPosition = staticPosition;
            
            PlayInternal(eventDescription);
        }

        private void PlayInternal(EventDescription eventDescription)
        {
            eventDescription.getPath(out string path);
            
            if (!eventDescription.isValid())
            {
                eventDescription.getID(out GUID guid);
                Debug.LogError($"Trying to play invalid FMOD Event guid: '{guid}' path:'{path}'");
                return;
            }

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
            
            if (spatializationType == SpatializationTypes.Transform)
            {
                if (spatializationTransform != null)
                {
                    Instance.set3DAttributes(RuntimeUtils.To3DAttributes(spatializationTransform));
                    RuntimeManager.AttachInstanceToGameObject(Instance, spatializationTransform);
                }
                else
                {
                    Debug.LogError($"Tried to spatialize FMOD Event '{Path}' to transform " +
                                   $"'{spatializationTransform}' which has seemingly been destroyed already.");
                }
            }
            else if (spatializationType == SpatializationTypes.StaticPosition)
            {
                Instance.set3DAttributes(RuntimeUtils.To3DAttributes(spatializationStaticPosition));
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
            
            UpdateTimelineMarkerCallbackState(true);
            
            if (Instance.isValid())
            {
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
        
        private void UpdateTimelineMarkerCallbackState(bool forceRemove = false)
        {
            bool shouldRegisterTimelineMarkerReachedCallback = timelineMarkerListenerCount > 0 ||
                                            (timelineEventIdToHandlers != null && timelineEventIdToHandlers.Count > 0);
            if (forceRemove)
                shouldRegisterTimelineMarkerReachedCallback = false;
            
            if (!hasRegisteredTimelineMarkerReachedCallback && shouldRegisterTimelineMarkerReachedCallback)
            {
                // Timeline callback code (see: https://www.fmod.com/docs/2.00/unity/examples-timeline-callbacks.html)
                
                // Explicitly create the delegate object and assign it to a member so it doesn't get freed
                // by the garbage collected while it's being used
                // NOTE: The documentation of 2.00 does this but 2.03 does not, but I am finding a very nasty hard
                // editor crash that seemingly only occurs when this delegate object is not created / used...
                timelineEventCallback = new EVENT_CALLBACK(OnTimelineMarkerReached);
                
                Instance.setCallback(timelineEventCallback, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
                // -----------------------------------------------------------------------------------------------------
            }
            else if (hasRegisteredTimelineMarkerReachedCallback && !shouldRegisterTimelineMarkerReachedCallback)
            {
                Instance.setCallback(null, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
                Instance.setUserData(IntPtr.Zero);
            }

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
        
        // NOTE: This works in the editor but not in a build, because it's not static, which IL2CPP can't marshal
        [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
        private RESULT OnTimelineMarkerReached(EVENT_CALLBACK_TYPE type, IntPtr @event, IntPtr parameterPtr)
        {
            // ---------------------------------------------------------------------------------------------------------
            // Awful FMOD timeline callback code.
            // See: https://www.fmod.com/docs/2.00/unity/examples-timeline-callbacks.html
            // Note that it is quite different from 2.03:
            // https://www.fmod.com/docs/2.03/unity/examples-timeline-callbacks.html
            // I got this to work on 2.03 but we may have to go back and check that this still works on 2.00
            string id = string.Empty;
            switch (type)
            {
                // Unused
                //case FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT:
                
                case FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER:
                {
                    TIMELINE_MARKER_PROPERTIES parameter = (FMOD.Studio.TIMELINE_MARKER_PROPERTIES)Marshal
                        .PtrToStructure(parameterPtr, typeof(FMOD.Studio.TIMELINE_MARKER_PROPERTIES));
                    
                    id = parameter.name;
                    break;
                }
                
                // Unused
                case FMOD.Studio.EVENT_CALLBACK_TYPE.DESTROYED: break;
            }
            // ---------------------------------------------------------------------------------------------------------
            
            // Buffer the timeline marker event so we can handle it on the main thread.
            timelineMarkersReached.Enqueue(id);
            
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
        
        public FmodAudioPlayback SetParameter(string name, float value, bool ignoreSeekSpeed = false)
        {
            if (Instance.isValid())
                Instance.setParameterByName(name, value, ignoreSeekSpeed);
            return this;
        }

        public FmodAudioPlayback SetParameter(PARAMETER_ID id, float value, bool ignoreSeekSpeed = false)
        {
            if (Instance.isValid())
                Instance.setParameterByID(id, value, ignoreSeekSpeed);
            return this;
        }

        public FmodAudioPlayback SetParameterLabel(string name, string label, bool ignoreSeekSpeed = false)
        {
            if (Instance.isValid())
                Instance.setParameterByNameWithLabel(name, label, ignoreSeekSpeed);
            return this;
        }
    }
}


#endif // FMOD_AUDIO_SYNTAX
