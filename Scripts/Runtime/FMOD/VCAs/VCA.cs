using FMOD.Studio;
using FMODUnity;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Utility for accessing the VCAs more conveniently.
    /// </summary>
    public class VCA
    {
        private readonly string path;
        
        /// <summary>
        /// NOTE: Seems like we can't cache this for some reason that's related to domain reloading. Not sure yet why.
        /// </summary>
        private FMOD.Studio.VCA FmodVca => RuntimeManager.GetVCA(path);

        public float VolumeLinear
        {
            get => FmodVca.getVolume();
            set => FmodVca.setVolume(value);
        }

        public VCA(string path)
        {
            this.path = path;
        }
    }
}
