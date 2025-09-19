#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
namespace RoyTheunissen.FMODSyntax.Callbacks
{
    public interface IOnFmodPlayback
    {
        void OnFmodPlaybackRegistered(FmodAudioPlayback playback);
        void OnFmodPlaybackUnregistered(FmodAudioPlayback playback);
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
