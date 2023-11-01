using System;
using System.IO;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using RoyTheunissen.FMODSyntax.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Non-generic base class for AudioFmodPlayback to apply as a type constraint.
    /// </summary>
    public abstract class FmodAudioPlaybackBase
    {
    }
    
    /// <summary>
    /// Playback for a playable FMOD audio event. Allows you to update its parameters, similar to a AudioUnityPlayback
    /// instance. Produced by calling Play() on an AudioFmodConfig, which are specified in FmodEvents.Events
    /// </summary>
    public abstract class FmodAudioPlayback : FmodAudioPlaybackBase, IAudioPlayback
    {
        private EventInstance instance;
        protected EventInstance Instance => instance;

        private EventDescription eventDescription;
        
        private bool isOneshot = false;

        public bool CanBeCleanedUp
        {
            get
            {
                if (!Instance.isValid())
                    return true;
                
                Instance.getPlaybackState(out PLAYBACK_STATE playbackState);
                return playbackState == PLAYBACK_STATE.STOPPED;
            }
        }

        private string name;
        public string Name => name;

        private string searchKeywords;
        public string SearchKeywords => searchKeywords;

        public bool IsOneshot => isOneshot;
        public float NormalizedProgress
        {
            get
            {
                if (!eventDescription.isValid() || !instance.isValid())
                    return 0.0f;
                
                eventDescription.getLength(out int timelineDurationInMilliseconds);
                instance.getTimelinePosition(out int timelinePositionInMilliSeconds);
                return (float)(timelinePositionInMilliSeconds / (double)timelineDurationInMilliseconds);
            }
        }

        public float Volume
        {
            get
            {
                if (!eventDescription.isValid() || !instance.isValid())
                    return 1.0f;
                instance.getVolume(out float volume);
                return volume;
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
            searchKeywords = path.Substring(path.IndexOf("/", StringComparison.Ordinal) + 1).Replace('/', ',');
            
            name = Path.GetFileName(path);

            this.eventDescription = eventDescription;
            eventDescription.createInstance(out instance);
            
            if (source != null)
            {
                instance.set3DAttributes(RuntimeUtils.To3DAttributes(source));
                RuntimeManager.AttachInstanceToGameObject(instance, source);
            }
            
            // Cache properties.
            bool isSnapshot;
            eventDescription.isSnapshot(out isSnapshot);
            if (!isSnapshot)
                eventDescription.isOneshot(out isOneshot);

            InitializeParameters();

            instance.start();

            FmodSyntaxSystem.RegisterActiveEventPlayback(this);
        }

        protected virtual void InitializeParameters()
        {
            // We need to pass our instance on to our parameters so we can set its values correctly.
        }

        public void Stop()
        {
            if (instance.isValid())
                instance.stop(STOP_MODE.ALLOWFADEOUT);
        }

        public void Cleanup()
        {
            if (instance.isValid())
            {
                RuntimeManager.DetachInstanceFromGameObject(instance);
                if (eventDescription.isValid() && isOneshot)
                {
                    instance.release();
                    instance.clearHandle();
                }
            }

            FmodSyntaxSystem.UnregisterActiveEventPlayback(this);
        }
    }
}
