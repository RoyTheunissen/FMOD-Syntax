using System;

namespace RoyTheunissen.FMODSyntax.Callbacks
{
    [Obsolete("IOnFmodPlayback has been renamed to IOnFmodPlaybackRegistered for clarity. Please use that instead.")]
    public interface IOnFmodPlayback : IOnFmodPlaybackRegistered
    {
    }
    
    /// <summary>
    /// Interface to use when you want a callback whenever an FMOD event playback is (un)registered.
    /// </summary>
    public interface IOnFmodPlaybackRegistered 
    {
        void OnFmodPlaybackRegistered(FmodAudioPlayback playback);
        void OnFmodPlaybackUnregistered(FmodAudioPlayback playback);
    }
}
