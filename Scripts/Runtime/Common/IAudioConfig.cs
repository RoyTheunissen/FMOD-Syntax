using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public interface IAudioConfig 
    {
        string Name { get; }
        string Path { get; }
        
        IAudioPlayback Play(Transform source = null);
        IAudioPlayback Play(Vector3 position);
    }
}
