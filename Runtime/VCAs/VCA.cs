using System;
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
        
        [NonSerialized] private FMOD.Studio.VCA cachedFmodVCA;
        [NonSerialized] private bool didCacheFmodVCA;
        private FMOD.Studio.VCA FmodVca
        {
            get
            {
                if (!didCacheFmodVCA)
                {
                    didCacheFmodVCA = true;
                    cachedFmodVCA = RuntimeManager.GetVCA(path);
                }
                return cachedFmodVCA;
            }
        }

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
