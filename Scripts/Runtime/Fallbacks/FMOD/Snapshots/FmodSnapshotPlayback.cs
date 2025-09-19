#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using FMOD.Studio;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Non-generic base class for FmodSnapshotPlayback to apply as a type constraint.
    /// </summary>
    [System.Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public abstract class FmodSnapshotPlaybackBase : FmodPlayablePlaybackBase
    {
        /// <summary>
        /// For snapshots it's very useful to define a parameter to control the Intensity setting of the snapshot and
        /// thus blend it in and out. This can easily be created by right-clicking the Intensity control in the right
        /// of the snapshot editor and choosing Expose As Parameter.
        /// NOTE: By default the intensity parameter's values go from 0 - 100 so we respect that.
        /// Use IntensityNormalized if you prefer to set it as number between 0 and 1.
        /// </summary>
        public float Intensity
        {
            get => 0.0f;
            set
            {
            }
        }

        public float IntensityNormalized
        {
            get => 0.0f;
            set
            {
            }
        }
    }
    
    /// <summary>
    /// Playback of a snapshot. Sort of like an event, but simpler.
    /// </summary>
    public abstract class FmodSnapshotPlayback : FmodSnapshotPlaybackBase, IFmodPlayback
    {
        public void Play(EventDescription eventDescription)
        {
        }
        
        public void Stop()
        {
        }

        public override void Cleanup()
        {
        }
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
