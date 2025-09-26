// #define DEBUG_AUDIO_EVENT_CONFIG_LOADING

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

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

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
    
        [SerializeField] private AudioEventConfigPropertyFloat volumeFactor = new(1.0f, false);
        public AudioEventConfigPropertyFloat VolumeFactor => volumeFactor;
        
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
            bool isActuallyPlaying = Application.isPlaying;
            
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                isActuallyPlaying = true;
#endif // UNITY_EDITOR
            
            if (!isActuallyPlaying)
                return;
            
            pathToAudioEventConfig[Path] = this;
#if DEBUG_AUDIO_EVENT_CONFIG_LOADING
            Debug.Log($"<color=green>LOADED AUDIO EVENT '{Path}'</color>");
#endif // DEBUG_AUDIO_EVENT_CONFIG_LOADING
        }
        
        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;
            
            pathToAudioEventConfig.Remove(Path);
            
#if DEBUG_AUDIO_EVENT_CONFIG_LOADING
            Debug.Log($"<color=red>UNLOADED AUDIO EVENT '{Path}'</color>");
#endif // DEBUG_AUDIO_EVENT_CONFIG_LOADING
        }

        public static bool TryGetLoadedConfig<ConfigType>(string path, out ConfigType config)
            where ConfigType : UnityAudioEventConfigAssetBase
        {
            bool success = pathToAudioEventConfig.TryGetValue(path, out UnityAudioEventConfigAssetBase result);
            config = result as ConfigType;
            return success;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void ClearLoadedConfigs()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChangedEvent;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChangedEvent;
        }

        private static void HandlePlayModeStateChangedEvent(PlayModeStateChange change)
        {
            // Need to clear this
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                pathToAudioEventConfig.Clear();
#if DEBUG_AUDIO_EVENT_CONFIG_LOADING
                Debug.Log($"<color=red>UNLOADED ALL AUDIO EVENTS BECAUSE EXITING EDIT MODE</color>");
#endif // DEBUG_AUDIO_EVENT_CONFIG_LOADING
            }
        }
#endif // UNITY_EDITOR
        
        
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
