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
        private enum ConfigStatuses
        {
            NotLoaded,
            Loaded,
            RequiresLazyLoading,
        }
        
        [NonSerialized] private ConfigType cachedConfig;
        [NonSerialized] private ConfigStatuses configStatus = ConfigStatuses.NotLoaded;
        private ConfigType Config
        {
            get
            {
                TryLoadConfig();
                return cachedConfig;
            }
        }

        protected UnityAudioEventStaticAccessConfig(string guid, string path, string name)
            : base(guid, path, name)
        {
        }

        public PlaybackType Play(Transform source = null)
        {
            TryLoadConfig();

            if (configStatus == ConfigStatuses.RequiresLazyLoading)
            {
                PlaybackType placeholder = UnityAudioPlayback.PlayWithLazyLoading<PlaybackType>(source);
                
                Debug.LogWarning($"Tried to play audio event '{Path}' via code but it was not loaded. Will load " +
                                 $"it now, but you might notice a delay. Consider grouping the audio configs and " +
                                 $"loading the entire group upfront before you need them.");
                
                UnityAudioSyntaxSystem.TryLoadAudioEventConfigFromAddressables<ConfigType>(Path, config =>
                {
                    placeholder.CompleteInitialization(config);
                    
                    cachedConfig = config;
                    configStatus = ConfigStatuses.Loaded;
                });

                return placeholder;
            }
            
            return UnityAudioPlayback.Play<PlaybackType>(Config, source);
        }

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return Play(source);
        }

        private void TryLoadConfig()
        {
            if (configStatus != ConfigStatuses.NotLoaded)
                return;
            
            UnityAudioSyntaxSystem.LoadAudioEventConfigResults result =
                UnityAudioSyntaxSystem.TryLoadAudioEventConfigAtRuntime(Path, out cachedConfig);
            switch (result)
            {
                case UnityAudioSyntaxSystem.LoadAudioEventConfigResults.Success:
                    configStatus = ConfigStatuses.Loaded;
                    break;
                
                case UnityAudioSyntaxSystem.LoadAudioEventConfigResults.RequiresLazyLoading:
                    configStatus = ConfigStatuses.RequiresLazyLoading;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
#endif // !UNITY_AUDIO_SYNTAX
