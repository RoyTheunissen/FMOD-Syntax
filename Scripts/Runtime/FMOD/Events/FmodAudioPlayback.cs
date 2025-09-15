using System;
using System.IO;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using RoyTheunissen.FMODSyntax.UnityAudioSyntax;
using UnityEngine;
using Debug = UnityEngine.Debug;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace RoyTheunissen.FMODSyntax
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
                
                if (timelineMarkerListenerCount == 1)
                    Instance.setCallback(OnTimelineMarkerReached, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
            }
            remove
            {
                timelineMarkerReachedEvent -= value;
                timelineMarkerListenerCount--;
                
                if (timelineMarkerListenerCount == 0)
                    Instance.setCallback(null, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
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

            AudioSyntaxSystem.RegisterActiveEventPlayback(this);
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

            AudioSyntaxSystem.UnregisterActiveEventPlayback(this);
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
        
        private RESULT OnTimelineMarkerReached(EVENT_CALLBACK_TYPE type, IntPtr @event, IntPtr parameterPtr)
        {
            TIMELINE_MARKER_PROPERTIES parameter = (TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(
                parameterPtr, typeof(TIMELINE_MARKER_PROPERTIES));
            
            timelineMarkerReachedEvent?.Invoke(this, parameter.name);
            
            return RESULT.OK;
        }
    }
}
