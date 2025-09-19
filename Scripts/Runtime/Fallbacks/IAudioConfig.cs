#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    [System.Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public interface IAudioConfig 
    {
        string Name { get; }
        string Path { get; }
        
        IAudioPlayback Play(Transform source = null);
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
