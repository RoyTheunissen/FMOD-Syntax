#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
namespace RoyTheunissen.FMODSyntax.Callbacks
{
    [System.Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public interface IOnFmodPlayback
    {
        void OnFmodPlaybackRegistered(FmodAudioPlayback playback);
        void OnFmodPlaybackUnregistered(FmodAudioPlayback playback);
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
