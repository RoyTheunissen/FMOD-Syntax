using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    public interface IAudioConfig 
    {
        bool IsAssigned { get; }
        string Name { get; }
        string Path { get; }
        
        IAudioPlayback Play(Transform source = null);
    }
}
