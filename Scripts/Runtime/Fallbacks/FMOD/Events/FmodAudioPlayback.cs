#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
using FMOD.Studio;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Playback for a playable FMOD audio event. Allows you to update its parameters.
    /// Produced by calling Play() on an AudioFmodConfig, which are specified in FmodEvents.AudioEvents
    /// </summary>
    [System.Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public abstract class FmodAudioPlayback : FmodPlayablePlaybackBase, IFmodAudioPlayback
    {
        private bool isOneshot = false;
        public bool IsOneshot
        {
            get => isOneshot;
            protected set => isOneshot = value;
        }
        
        public delegate void TimelineMarkerReachedHandler(FmodAudioPlayback playback, string markerName);
        public event TimelineMarkerReachedHandler TimelineMarkerReachedEvent
        {
            add
            {
            }
            remove
            {
            }
        }
        
        public void Play(EventDescription eventDescription, Transform source)
        {
        }

        public void Stop()
        {
        }

        public override void Cleanup()
        {
        }
        
        public FmodAudioPlayback SubscribeToTimelineMarkerReachedEvent(TimelineMarkerReachedHandler handler)
        {
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
            return this;
        }
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
