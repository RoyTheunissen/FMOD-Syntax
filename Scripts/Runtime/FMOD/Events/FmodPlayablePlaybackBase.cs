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
        /// <summary>
        /// Useful for things like quickly filtering out a subgroup of active events while debugging.
        /// </summary>
        public string SearchKeywords
        {
            get => searchKeywords;
            protected set => searchKeywords = value;
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
        
        protected virtual void InitializeParameters()
        {
            // We need to pass our instance on to our parameters so we can set its values correctly.
        }
        
        public void MoveTowardsVolume(float target, float maxDelta)
        {
            Volume = Mathf.MoveTowards(Volume, target, maxDelta);
        }
        
        public void SmoothDampTowardsVolume(float target, float duration)
        {
            // Need to explicitly check for this or it can return NaN if Time.timeScale is 0. Yes, really.
            // https://discussions.unity.com/t/mathf-smoothdamp-assigns-nan/383635/13
            if (!Mathf.Approximately(Time.deltaTime, 0.0f))
                Volume = Mathf.SmoothDamp(Volume, target, ref smoothDampVolumeVelocity, duration);
        }

        public abstract void Cleanup();
    }
}
