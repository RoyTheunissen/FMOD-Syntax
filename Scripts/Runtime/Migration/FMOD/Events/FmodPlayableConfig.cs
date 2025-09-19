#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using FMOD.Studio;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Base class for configs of Playables (Events and Snapshots).
    /// </summary>
    public abstract class FmodPlayableConfig
    {
        public string Id => string.Empty;
        
        /// <summary>
        /// NOTE: Seems like we can't cache this for some reason that's related to domain reloading. Not sure yet why.
        /// </summary>
        protected EventDescription EventDescription => default;

        public string Path => string.Empty;

        public string Name => string.Empty;

        public bool IsAssigned => false;

        public FmodPlayableConfig(string guid)
        {
        }

        public void Preload()
        {
        }
        
        public void Unload()
        {
        }
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
