namespace RoyTheunissen.AudioSyntax.Callbacks
{
    /// <summary>
    /// Interface to use when you want a callback whenever an audio event playback is (un)registered.
    /// </summary>
    public interface IOnAudioPlaybackRegistration
    {
        void OnAudioPlaybackRegistered(IAudioPlayback playback);
        void OnAudioPlaybackUnregistered(IAudioPlayback playback);
    }
}
