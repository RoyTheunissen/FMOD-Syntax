using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    public interface IAudioConfig 
    {
        bool IsAssigned { get; }
        
        IAudioPlayback Play(Transform source = null);
    }
}
