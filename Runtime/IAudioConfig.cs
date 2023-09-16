using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    public interface IAudioConfig 
    {
        IAudioPlayback Play(Transform source = null);
    }
}
