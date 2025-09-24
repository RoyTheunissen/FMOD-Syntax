#if UNITY_AUDIO_SYNTAX

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

#if SCRIPTABLE_OBJECT_COLLECTION
using BrunoMikoski.ScriptableObjectCollections;

    #if USE_COLLECTION_ITEM_PICKER_FOR_TAGS
    using BrunoMikoski.ScriptableObjectCollections.Picker;
    #endif
#endif

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Base class for one-off and continuous Unity audio event config assets.
    /// </summary>
    public abstract class UnityAudioEventConfigAssetBase : ScriptableObject, IAudioConfig
    {
        [SerializeField, HideInInspector] private string path;
        public string Path => path;
        
        [SerializeField] private AudioMixerGroup mixerGroup;
        public AudioMixerGroup MixerGroup => mixerGroup;
    
        [SerializeField] private float volumeFactor = 1.0f;
        public float VolumeFactor => volumeFactor;
        
#if SCRIPTABLE_OBJECT_COLLECTION && USE_COLLECTION_ITEM_PICKER_FOR_TAGS
        [SerializeField] private CollectionItemPicker<UnityAudioTag> tags = new();
#else
        #if SCRIPTABLE_OBJECT_COLLECTION
        [SOCItemEditorOptions(ShouldDrawGotoButton = false, ShouldDrawPreviewButton = false, LabelMode = LabelMode.NoLabel)]
        #endif // SCRIPTABLE_OBJECT_COLLECTION
        [SerializeField] private List<UnityAudioTag> tags = new();
#endif
        public IList<UnityAudioTag> Tags => tags;

        public string Name => name;

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return PlayGeneric(source);
        }

        public abstract UnityAudioPlayback PlayGeneric(Transform source = null, float volumeFactor = 1.0f);

#if UNITY_AUDIO_SYNTAX_ADDRESSABLES
        private static readonly Dictionary<string, UnityAudioEventConfigAssetBase> pathToAudioEventConfig = new();
        
        private void OnEnable()
        {
            bool alreadyExisted = pathToAudioEventConfig.ContainsKey(Path);
            if (alreadyExisted)
            {
                Debug.LogError(
                    $"Tried to register Audio Event Config '{Path}' as loaded but an Audio Event of that " +
                    $"type was already loaded. Something went wrong in the loading flow. Either a path was " +
                    $"duplicated or an asset was not unregistered properly. Will override the existing config.");
            }

            pathToAudioEventConfig[Path] = this;
        }
        
        private void OnDisable()
        {
            pathToAudioEventConfig.Remove(Path);
        }

        public static ConfigType GetLoadedConfig<ConfigType>(string path)
            where ConfigType : UnityAudioEventConfigAssetBase
        {
            pathToAudioEventConfig.TryGetValue(path, out UnityAudioEventConfigAssetBase result);
            return result as ConfigType;
        }
#endif // UNITY_AUDIO_SYNTAX_ADDRESSABLES
    }
    
    /// <summary>
    /// Generic base class for Unity one-off and continuous audio event configs. When told to play, a Playback instance
    /// is created and returned. You can then use this to manipulate the audio.
    /// </summary>
    /// <typeparam name="PlaybackType">The type of Playback instance that should be created when played.</typeparam>
    public abstract class UnityAudioEventConfigAsset<PlaybackType> : UnityAudioEventConfigAssetBase
        where PlaybackType : UnityAudioPlayback, new()
    {
        public PlaybackType Play(float volumeFactor = 1.0f)
        {
            return Play(null, volumeFactor);
        }
        
        public PlaybackType Play(Transform origin, float volumeFactor = 1.0f)
        {
            return UnityAudioPlayback.Play<PlaybackType>(this, origin, volumeFactor);
        }

        public override UnityAudioPlayback PlayGeneric(Transform source = null, float volumeFactor = 1.0f)
        {
            return Play(source, volumeFactor);
        }
    }
}

#else
public abstract class UnityAudioEventConfigAssetBase : UnityEngine.ScriptableObject
{
}
#endif // !UNITY_AUDIO_SYNTAX
