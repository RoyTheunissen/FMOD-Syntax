#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Non-generic base class for FmodSnapshotConfig to apply as a type constraint.
    /// </summary>
    [System.Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public abstract class FmodSnapshotConfigBase : FmodPlayableConfig
    {
        protected FmodSnapshotConfigBase(string guid) : base(guid)
        {
        }

        public abstract FmodSnapshotPlayback PlayGeneric();
    }
    
    /// <summary>
    /// Config for a playable FMOD snapshot. Returns a playback instance of the specified snapshot so you can stop it
    /// from there. Very similar to an FMOD event, but represents a certain set of mixer property values. Note that
    /// unlike events, snapshots do not have any parameters. As such, we do not need to generate new types of snapshots,
    /// we just need to generate snapshots with the appropriate GUIDs. 
    /// </summary>
    [System.Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public abstract class FmodSnapshotConfig<PlaybackType> : FmodSnapshotConfigBase
        where PlaybackType : FmodSnapshotPlayback
    {
        public FmodSnapshotConfig(string guid) : base(guid)
        {
        }
        
        public abstract PlaybackType Play();

        public override FmodSnapshotPlayback PlayGeneric() => Play();
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
