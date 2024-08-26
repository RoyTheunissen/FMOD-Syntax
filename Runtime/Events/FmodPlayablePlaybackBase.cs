using FMOD.Studio;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Non-generic base class for the playback of Playables (Events / Snapshots).
    /// </summary>
    public abstract class FmodPlayablePlaybackBase
    {
        private EventInstance instance;
        protected EventInstance Instance
        {
            get => instance;
            set => instance = value;
        }

        private EventDescription eventDescription;
        protected EventDescription EventDescription
        {
            get => eventDescription;
            set => eventDescription = value;
        }

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
        public string Name
        {
            get => name;
            protected set => name = value;
        }

        private string searchKeywords;
        public string SearchKeywords
        {
            get => searchKeywords;
            protected set => searchKeywords = value;
        }

        public bool IsOneshot
        {
            get => isOneshot;
            protected set => isOneshot = value;
        }

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
            set
            {
                if (!eventDescription.isValid() || !instance.isValid())
                    return;
                
                instance.setVolume(Mathf.Max(value, 0.0f));
            }
        }
        
        private float smoothDampVolumeVelocity;
        
        public void MoveTowardsVolume(float target, float maxDelta)
        {
            Volume = Mathf.MoveTowards(Volume, target, maxDelta);
        }
        
        public void SmoothDampTowardsVolume(float target, float duration)
        {
            Volume = Mathf.SmoothDamp(Volume, target, ref smoothDampVolumeVelocity, duration);
        }

        public abstract void Cleanup();
    }
}
