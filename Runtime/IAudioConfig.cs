using UnityEngine;

namespace RoyTheunissen.FMODWrapper
{
    public interface IAudioConfig 
    {
        IAudioPlayback Play(Transform source = null);
    }
}
