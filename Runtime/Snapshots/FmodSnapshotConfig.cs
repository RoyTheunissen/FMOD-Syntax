namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Config for a playable FMOD snapshot. Returns a playback instance of the specified snapshot so you can stop it
    /// from there. Very similar to an FMOD event, but represents a certain set of mixer property values. Note that
    /// unlike events, snapshots do not have any parameters. As such, we do not need to generate new types of snapshots,
    /// we just need to generate snapshots with the appropriate GUIDs. 
    /// </summary>
    public sealed class FmodSnapshotConfig : FmodPlayableConfig
    {
        public FmodSnapshotConfig(string guid) : base(guid)
        {
        }

        public FmodSnapshotPlayback Play()
        {
            FmodSnapshotPlayback instance = new FmodSnapshotPlayback();
            instance.Play(EventDescription);
            return instance;
        }
    }
}
