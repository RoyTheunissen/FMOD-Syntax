using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using Debug = UnityEngine.Debug;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Non-generic base class for AudioFmodPlayback to apply as a type constraint.
    /// </summary>
    public abstract class FmodAudioPlaybackBase : FmodPlayablePlaybackBase
    {
        private bool isOneshot = false;
        public bool IsOneshot
        {
            get => isOneshot;
            protected set => isOneshot = value;
        }
    }
    
    /// <summary>
    /// Playback for a playable FMOD audio event. Allows you to update its parameters.
    /// Produced by calling Play() on an AudioFmodConfig, which are specified in FmodEvents.AudioEvents
    /// </summary>
    public abstract class FmodAudioPlayback : FmodAudioPlaybackBase, IAudioPlayback
    {
        // ------------------------------------------------------------------------------------------------------------
        // Hideous timeline callback functionality. I do not approve, but this is the way FMOD says it needs to be done:
        // https://www.fmod.com/docs/2.03/unity/examples-timeline-callbacks.html
        // This class exists so that it can be pinned in memory, which in turn is then passed along to a *static*
        // callback method for timeline events *that happens on a separate thread, not the Unity main thread*. Then
        // we grab this object, queue up the timeline markers that are reached, and then on the main thread,
        // on the next Update, the playback instance will fire all the timeline events that were buffered, to ensure
        // that the callbacks happen on the main Unity thread. This thing with pinning the class in memory is not
        // necessary to get it to work in the editor, but it IS necessary to get it to work in IL2CPP builds.
        // So while this may all look very "un-C#" and bloated and unnecessary, I assure you that every part of this
        // is necessary for FMOD's code to work.
        
        // Variables that are modified in the callback need to be part of a seperate class.
        // This class needs to be 'blittable' otherwise it can't be pinned in memory.
        private class TimelineInfo
        {
            public readonly ConcurrentQueue<string> TimelineMarkersReached = new();
        }
        private TimelineInfo timelineInfo;
        private GCHandle timelineInfoHandle;
        private EVENT_CALLBACK timelineEventCallback;
        // ------------------------------------------------------------------------------------------------------------
        
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
        
        public void Update()
        {
            if (hasRegisteredTimelineMarkerReachedCallback)
                FireBufferedTimelineMarkerEvents();
        }

        private void FireBufferedTimelineMarkerEvents()
        {
            // Timeline marker callbacks happen on a separate FMOD audio thread, so to safely pass it along to Unity,
            // we need to buffer the timeline marker events and fire them on the next update of the main thread.
            while (timelineInfo.TimelineMarkersReached.TryDequeue(out string id))
            {
                timelineMarkerReachedEvent?.Invoke(this, id);
            }
        }

        
        public void Play(EventDescription eventDescription, Transform source)
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
            
            Name = Path.GetFileName(path);

            EventDescription = eventDescription;
            eventDescription.createInstance(out EventInstance newInstance);
            Instance = newInstance;
            
            if (source != null)
            {
                Instance.set3DAttributes(RuntimeUtils.To3DAttributes(source));
                RuntimeManager.AttachInstanceToGameObject(Instance, source);
            }
            
            // Cache properties.
            eventDescription.isOneshot(out bool isOneshotResult);
            IsOneshot = isOneshotResult;

            InitializeParameters();

            Instance.start();

            FmodSyntaxSystem.RegisterActiveEventPlayback(this);
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
                Instance.setCallback(null, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
                
                Instance.stop(STOP_MODE.IMMEDIATE);
                
                RuntimeManager.DetachInstanceFromGameObject(Instance);
                if (EventDescription.isValid())
                {
                    Instance.release();
                    Instance.clearHandle();
                }
            }

            FmodSyntaxSystem.UnregisterActiveEventPlayback(this);
        }
        
        private void UpdateTimelineMarkerCallbackState(bool forceRemove = false)
        {
            bool shouldRegisterTimelineMarkerReachedCallback = timelineMarkerListenerCount > 0;
            if (forceRemove)
                shouldRegisterTimelineMarkerReachedCallback = false;
            
            if (!hasRegisteredTimelineMarkerReachedCallback && shouldRegisterTimelineMarkerReachedCallback)
            {
                // -----------------------------------------------------------------------------------------------------
                // Timeline callback code (see the comment section at the top of this file)
                timelineInfo = new TimelineInfo();
                
                // Explicitly create the delegate object and assign it to a member so it doesn't get freed
                // by the garbage collected while it's being used
                // NOTE: The documentation of 2.00 does this but 2.03 does not, but I am finding a very nasty hard
                // editor crash that seemingly only occurs when this delegate object is not created / used,
                // so do not remove the delegate object that wraps the method...
                timelineEventCallback = new EVENT_CALLBACK(OnTimelineMarkerReached);
                
                // Pin the class that will store the data modified during the callback
                timelineInfoHandle = GCHandle.Alloc(timelineInfo);
                // Pass the object through the userdata of the instance
                Instance.setUserData(GCHandle.ToIntPtr(timelineInfoHandle));
                
                Instance.setCallback(timelineEventCallback, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
                // -----------------------------------------------------------------------------------------------------
            }
            else if (hasRegisteredTimelineMarkerReachedCallback && !shouldRegisterTimelineMarkerReachedCallback)
            {
                Instance.setCallback(null, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
                Instance.setUserData(IntPtr.Zero);
                timelineInfoHandle.Free();
            }

            hasRegisteredTimelineMarkerReachedCallback = shouldRegisterTimelineMarkerReachedCallback;
        }

        /// <summary>
        /// Fluid method for subscribing to timeline events so you don't have to save the playback to a variable first
        /// if you don't want to (for example for one-off sounds).
        /// </summary>
        public FmodAudioPlayback SubscribeToTimelineMarkerReachedEvent(TimelineMarkerReachedHandler handler)
        {
            TimelineMarkerReachedEvent += handler;
            return this;
        }
        
        /// <summary>
        /// Fluid method for unsubscribing from timeline events so you don't have to save the playback to a variable
        /// first if you don't want to (for example for one-off sounds).
        /// </summary>
        public FmodAudioPlayback UnsubscribeFromTimelineMarkerReachedEvent(TimelineMarkerReachedHandler handler)
        {
            TimelineMarkerReachedEvent -= handler;
            return this;
        }
        
        // -------------------------------------------------------------------------------------------------------------
        // Awful FMOD timeline callback code. Do not change it too much, it is written in this really bizarre way
        // for very complicated and undocumented reasons and changing small things is likely to break things.
        [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
        private static RESULT OnTimelineMarkerReached(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
        {
            EventInstance instance = new(instancePtr);
            
            // Retrieve the user data
            IntPtr timelineInfoPtr;
            RESULT result = instance.getUserData(out timelineInfoPtr);
            if (result != RESULT.OK)
            {
                Debug.LogError("Timeline Callback error: " + result);
                return RESULT.OK;
            }

            if (timelineInfoPtr == IntPtr.Zero)
            {
                Debug.LogError("Bogus Timeline Marker reached: timeline info pointer was NULL.");
                return RESULT.OK;
            }
            
            // Get the object to store beat and marker details
            GCHandle timelineHandle = GCHandle.FromIntPtr(timelineInfoPtr);
            TimelineInfo timelineInfo = (TimelineInfo)timelineHandle.Target;

            if (timelineInfo == null)
            {
                Debug.LogError("Bogus Timeline Marker reached: timeline handle could not be cast to timeline handle.");
                return RESULT.OK;
            }
            
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
                
                case FMOD.Studio.EVENT_CALLBACK_TYPE.DESTROYED:
                    // Now the event has been destroyed, unpin the timeline memory so it can be garbage collected
                    timelineHandle.Free();
                    break;
            }
            
            // Buffer the timeline marker event so we can handle it on the main thread.
            timelineInfo.TimelineMarkersReached.Enqueue(id);
            
            return RESULT.OK;
        }
        // -------------------------------------------------------------------------------------------------------------
    }
}
