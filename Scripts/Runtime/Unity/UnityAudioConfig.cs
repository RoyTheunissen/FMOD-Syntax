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
    /// Base class for one-off and continuous Unity audio configs.
    /// </summary>
    public abstract class UnityAudioConfigBase : ScriptableObject, IAudioConfig
    {
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

        public string Path
        {
            get
            {
                // TODO: Do we need to support this for Unity Audio Events?
                throw new System.NotImplementedException();
            }
        }

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return PlayGeneric(source);
        }

        public abstract UnityAudioPlayback PlayGeneric(Transform source = null, float volumeFactor = 1.0f);
    }
    
    /// <summary>
    /// Generic base class for Unity one-off and continuous audio configs. When told to play, a Playback instance is
    /// created and returned. You can then use this to manipulate the sound.
    /// </summary>
    /// <typeparam name="PlaybackType">The type of Playback instance that sound be created when played.</typeparam>
    public abstract class UnityAudioConfigGeneric<PlaybackType> : UnityAudioConfigBase
        where PlaybackType : UnityAudioPlayback, new()
    {
        public PlaybackType Play(float volumeFactor = 1.0f)
        {
            return Play(null, volumeFactor);
        }
        
        public PlaybackType Play(Transform origin, float volumeFactor = 1.0f)
        {
            PlaybackType playback = new PlaybackType();
            
            AudioSource audioSource = UnityAudioSyntaxSystem.Instance.GetAudioSourceForPlayback(this);;
            playback.Initialize(this, origin, volumeFactor, audioSource);
            
#if DEBUG_AUDIO_SOURCE_POOLING && UNITY_EDITOR
            audioSource.name = "AudioSource - " + playback;
#endif // DEBUG_AUDIO_SOURCE_POOLING

            UnityAudioSyntaxSystem.RegisterActiveEventPlayback(playback);

            return playback;
        }

        public override UnityAudioPlayback PlayGeneric(Transform source = null, float volumeFactor = 1.0f)
        {
            return Play(source, volumeFactor);
        }
    }
}

#else
public abstract class UnityAudioConfigBase : UnityEngine.ScriptableObject
{
}
#endif // !UNITY_AUDIO_SYNTAX
