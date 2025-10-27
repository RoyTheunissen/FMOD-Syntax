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
            RequiresAsynchronousLoading,
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


        private bool TryPlayAsynchronouslyIfNecessary(
            SpatializationTypes spatializationType, Transform source, Vector3 position, out PlaybackType playback)
        {
            if (configStatus != ConfigStatuses.RequiresAsynchronousLoading)
            {
                playback = default;
                return false;
            }
                
#if UNITY_AUDIO_SYNTAX_ADDRESSABLES
                PlaybackType placeholder;

                switch (spatializationType)
                {
                    case SpatializationTypes.Global:
                    case SpatializationTypes.Transform:
                        placeholder = UnityAudioPlayback.PlayWithAsynchronousLoading<PlaybackType>(source);
                        break;
                    case SpatializationTypes.StaticPosition:
                        placeholder = UnityAudioPlayback.PlayWithAsynchronousLoading<PlaybackType>(position);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(spatializationType), spatializationType, null);
                }

                Debug.LogWarning($"Tried to play audio event '{Path}' via code but it was not loaded. Will load " +
                                 $"it now, but you might notice a delay. Consider grouping the audio configs and " +
                                 $"loading the entire group upfront before you need them.");
                
                UnityAudioSyntaxSystem.TryLoadAudioEventConfigFromAddressables<ConfigType>(Path, config =>
                {
                    placeholder.CompleteInitialization(config);
                    
                    cachedConfig = config;
                    configStatus = ConfigStatuses.Loaded;
                });

                playback = placeholder;
                return true;
#else
            Debug.LogWarning($"Config '{Path}' seemed to require asynchronous loading but the Addressables " +
                             $"package was not found. Something is wrong with the project setup.");
            playback = default;
            return false;
#endif // !UNITY_AUDIO_SYNTAX_ADDRESSABLES
        }

        public PlaybackType Play(Transform source = null)
        {
            TryLoadConfig();

            if (TryPlayAsynchronouslyIfNecessary(
                    source == null ? SpatializationTypes.Global : SpatializationTypes.Transform, source, Vector3.zero,
                    out PlaybackType playback))
            {
                return playback;
            }

            return UnityAudioPlayback.Play<PlaybackType>(Config, source);
        }

        public PlaybackType Play(Vector3 position)
        {
            TryLoadConfig();

            if (TryPlayAsynchronouslyIfNecessary(
                    SpatializationTypes.StaticPosition, null, position, out PlaybackType playback))
            {
                return playback;
            }

            return UnityAudioPlayback.Play<PlaybackType>(Config, position);
        }

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return Play(source);
        }

        IAudioPlayback IAudioConfig.Play(Vector3 position)
        {
            return Play(position);
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
                
                case UnityAudioSyntaxSystem.LoadAudioEventConfigResults.RequiresAsynchronousLoading:
                    configStatus = ConfigStatuses.RequiresAsynchronousLoading;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX
