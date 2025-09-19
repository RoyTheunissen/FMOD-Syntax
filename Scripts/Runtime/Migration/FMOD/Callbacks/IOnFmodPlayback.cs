using System;

namespace RoyTheunissen.FMODSyntax.Callbacks
{
    public interface IOnFmodPlayback
    {
        void OnFmodPlaybackRegistered(FmodAudioPlayback playback);
        void OnFmodPlaybackUnregistered(FmodAudioPlayback playback);
    }
}

