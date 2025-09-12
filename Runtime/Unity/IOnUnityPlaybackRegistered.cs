namespace RoyTheunissen.FMODSyntax.UnityAudioSyntax
{
    /// <summary>
    /// Interface to use when you want a callback whenever a Unity audio event playback is (un)registered.
    /// </summary>
    public interface IOnUnityPlaybackRegistered 
    {
        void OnUnityPlaybackRegistered(UnityAudioPlayback playback);
        void OnUnityPlaybackUnregistered(UnityAudioPlayback playback);
    }
}