#if FMOD_AUDIO_SYNTAX

using System;

namespace RoyTheunissen.AudioSyntax.Callbacks
{
    public interface IOnFmodPlayback
    {
        void OnFmodPlaybackRegistered(FmodAudioPlayback playback);
        void OnFmodPlaybackUnregistered(FmodAudioPlayback playback);
    }
}
#endif // FMOD_AUDIO_SYNTAX

