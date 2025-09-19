#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    public interface IAudioConfig 
    {
        string Name { get; }
        string Path { get; }
        
        IAudioPlayback Play(Transform source = null);
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
