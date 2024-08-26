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
    public abstract class FmodAudioPlaybackBase : FmodPlayablePlaybackBase
    {
    }
    
    /// <summary>
    /// Playback for a playable FMOD audio event. Allows you to update its parameters.
    /// Produced by calling Play() on an AudioFmodConfig, which are specified in FmodEvents.AudioEvents
    /// </summary>
    public abstract class FmodAudioPlayback : FmodAudioPlaybackBase, IAudioPlayback
    {
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
            eventDescription.isSnapshot(out bool isSnapshot);
            if (!isSnapshot)
            {
                eventDescription.isOneshot(out bool isOneshotResult);
                IsOneshot = isOneshotResult;
            }

            InitializeParameters();

            Instance.start();

            FmodSyntaxSystem.RegisterActiveEventPlayback(this);
        }

        protected virtual void InitializeParameters()
        {
            // We need to pass our instance on to our parameters so we can set its values correctly.
        }

        public void Stop()
        {
            if (Instance.isValid())
                Instance.stop(STOP_MODE.ALLOWFADEOUT);
        }

        public override void Cleanup()
        {
            if (Instance.isValid())
            {
                RuntimeManager.DetachInstanceFromGameObject(Instance);
                if (EventDescription.isValid() && IsOneshot)
                {
                    Instance.release();
                    Instance.clearHandle();
                }
            }

            FmodSyntaxSystem.UnregisterActiveEventPlayback(this);
        }
    }
}
