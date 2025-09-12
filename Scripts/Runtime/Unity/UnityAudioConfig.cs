#if UNITY_AUDIO_SYNTAX

using UnityEngine;
using UnityEngine.Audio;

namespace RoyTheunissen.FMODSyntax.UnityAudioSyntax
{
    /// <summary>
    /// Base class for one-off and continuous Unity audio configs.
    /// </summary>
    public abstract class UnityAudioConfigBase : ScriptableObject
    {
        [SerializeField] private AudioMixerGroup mixerGroup;
        public AudioMixerGroup MixerGroup => mixerGroup;
    
        [SerializeField] private float volumeFactor = 1.0f;
        public float VolumeFactor => volumeFactor;
        
        // TODO: Support this again
        // [FormerlySerializedAs("audioTags")]
        // [SerializeField] private CollectionItemPicker<AudioTag> tags = new CollectionItemPicker<AudioTag>();
        // public CollectionItemPicker<AudioTag> Tags => tags;
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

            AudioSyntaxSystem.RegisterActiveEventPlayback(playback);

            return playback;
        }
    }
}

#endif // UNITY_AUDIO_SYNTAX
