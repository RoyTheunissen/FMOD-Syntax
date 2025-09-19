#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using FMOD.Studio;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Non-generic base class for the playback of Playables (Events / Snapshots).
    /// </summary>
    public abstract class FmodPlayablePlaybackBase
    {
        protected EventInstance Instance
        {
            get => default;
            set
            {
            }
        }

        private EventDescription eventDescription;
        protected EventDescription EventDescription
        {
            get => default;
            set
            {
            }
        }

        public bool CanBeCleanedUp => false;
        
        public string Name
        {
            get => string.Empty;
            protected set
            {
            }
        }

        /// <summary>
        /// Useful for things like quickly filtering out a subgroup of active events while debugging.
        /// </summary>
        public string SearchKeywords
        {
            get => string.Empty;
            protected set
            {
            }
        }

        public float NormalizedProgress => 0.0f;

        public float Volume
        {
            get => 1.0f;
            set
            {
            }
        }
        
        protected virtual void InitializeParameters()
        {
        }
        
        public void MoveTowardsVolume(float target, float maxDelta)
        {
        }
        
        public void SmoothDampTowardsVolume(float target, float duration)
        {
        }

        public abstract void Cleanup();
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
