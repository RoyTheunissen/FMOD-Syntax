#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Utility for accessing the buses more conveniently.
    /// </summary>
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
