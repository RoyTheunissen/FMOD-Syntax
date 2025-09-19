#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using System;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Utility for accessing the buses more conveniently.
    /// </summary>
    [Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public class Bus
    {
        public FMOD.Studio.Bus FmodBus => default;

        public float VolumeLinear
        {
            get => 1.0f;
            set
            {
            }
        }

        public Bus(string path)
        {
        }
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
