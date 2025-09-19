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
