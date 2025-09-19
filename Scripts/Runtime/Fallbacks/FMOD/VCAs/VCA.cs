#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Utility for accessing the VCAs more conveniently.
    /// </summary>
    public class VCA
    {
        public float VolumeLinear
        {
            get => 1.0f;
            set
            {
            }
        }

        public VCA(string path)
        {
        }
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
