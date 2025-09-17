using System;

namespace RoyTheunissen.AudioSyntax
{
    [Flags]
    public enum AudioSyntaxSystems
    {
        Nothing = 0,
        UnityNativeAudio = 1 << 0,
        FMOD = 1 << 1,
        Everything = ~0,
    }
}