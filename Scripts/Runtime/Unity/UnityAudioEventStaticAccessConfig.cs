using System;
using UnityEngine;

#if UNITY_AUDIO_SYNTAX
namespace RoyTheunissen.AudioSyntax
{
    public abstract class UnityAudioEventStaticAccessConfig
    {
        private string guid;
        public string Guid => guid;

        private readonly string name;
        public string Name => name;
        
        private readonly string path;
        public string Path => path;
        
        public UnityAudioEventStaticAccessConfig(string guid, string path, string name)
        {
            this.guid = guid;
            this.path = path;
            this.name = name;
        }
    }
    
    /// <summary>
    /// Base class for Unity audio event configs that are generated so that you can reference them through code.
    /// Supposed to load a UnityAudioEventConfigAssetBase at runtime and use that to produce an instance of an
    /// UnityAudioPlayback which is responsible for handling the playback of an audio event.
    /// </summary>
    public abstract class UnityAudioEventStaticAccessConfig<ConfigType, PlaybackType>
        : UnityAudioEventStaticAccessConfig, IAudioConfig
        where ConfigType : UnityAudioEventConfigAssetBase
        where PlaybackType : UnityAudioPlayback, new()
    {
        [NonSerialized] private ConfigType cachedConfig;
        [NonSerialized] private bool didCacheConfig;
        protected ConfigType Config
        {
            get
            {
                if (!didCacheConfig)
                {
                    didCacheConfig = true;
                    cachedConfig = LoadConfig();
                }
                return cachedConfig;
            }
        }

        protected UnityAudioEventStaticAccessConfig(string guid, string path, string name)
            : base(guid, path, name)
        {
        }

        public PlaybackType Play(Transform source = null)
        {
            return UnityAudioPlayback.Play<PlaybackType>(Config, source);
        }

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return Play(source);
        }

        private ConfigType LoadConfig()
        {
            return UnityAudioSyntaxSystem.LoadAudioEventConfigAtRuntime<ConfigType>(Path);
        }
    }
}
#endif // !UNITY_AUDIO_SYNTAX
