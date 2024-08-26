namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Non-generic base class for FmodSnapshotConfig to apply as a type constraint.
    /// </summary>
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
    public abstract class FmodSnapshotConfig<PlaybackType> : FmodPlayableConfig
        where PlaybackType : FmodSnapshotPlayback
    {
        public FmodSnapshotConfig(string guid) : base(guid)
        {
        }
        
        public abstract PlaybackType Play();
    }
}
