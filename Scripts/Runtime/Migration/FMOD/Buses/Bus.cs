using System;
using FMODUnity;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Utility for accessing the buses more conveniently.
    /// </summary>
    public class Bus
    {
        private readonly string path;
        
        [NonSerialized] private FMOD.Studio.Bus cachedFmodBus;
        [NonSerialized] private bool didCacheFmodBus;

        public FMOD.Studio.Bus FmodBus
        {
            get
            {
                if (!didCacheFmodBus)
                {
                    didCacheFmodBus = true;
                    cachedFmodBus = RuntimeManager.GetBus(path);
                }
                return cachedFmodBus;
            }
        }

        public float VolumeLinear
        {
            get => FMOD.Studio.BusExtensions.getVolume(FmodBus);
            set => FmodBus.setVolume(value);
        }

        public Bus(string path)
        {
            this.path = path;
        }
    }
}
