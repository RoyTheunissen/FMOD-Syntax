namespace RoyTheunissen.FMODSyntax.Callbacks
{
    /// <summary>
    /// Interface to use when you want a callback whenever an FMOD event playback is (un)registered.
    /// </summary>
    public interface IOnFmodPlayback 
    {
        void OnFmodPlaybackRegistered(FmodAudioPlayback playback);
        void OnFmodPlaybackUnregistered(FmodAudioPlayback playback);
    }
}
