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

        // TODO: Do we need to support this for Unity Audio Events?
        // Not sure what the best way would be to go about it. This path depends on the asset's location in the project.
        // At editor time this is easy to determine, but at runtime there's no way to find out. We would have to cache
        // it. Is that desirable though? We *need* to use a path to be able to play Unity audio events from code, but
        // that works slightly differently. A custom class is generated, and that one caches the GUID, path and name.
        // We could do it, we could cache the path when you make the asset, the thing is that it would also have to
        // be updated when you move the asset, and also all the paths would have to be updated if you were to change
        // the audio event config base path in the Unity Audio Syntax System Settings config. So it's just a bunch of
        // extra work with no clear use case. If you need this, let me know and I will consider supporting it.
        public string Path
        {
            get
            {
                Debug.LogWarning($"Tried to ask for a path of a Unity Audio Event Config Asset '{name}'. " +
                                 $"This is not currently supported.", this);
                return "UNKNOWNPATH/" + name;
            }
        }

        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return PlayGeneric(source);
        }

        public abstract UnityAudioPlayback PlayGeneric(Transform source = null, float volumeFactor = 1.0f);
    }
    
    /// <summary>
    /// Generic base class for Unity one-off and continuous audio event configs. When told to play, a Playback instance
    /// is created and returned. You can then use this to manipulate the audio.
    /// </summary>
    /// <typeparam name="PlaybackType">The type of Playback instance that should be created when played.</typeparam>
    public abstract class UnityAudioEventConfigGeneric<PlaybackType> : UnityAudioEventConfigAssetBase
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
