#if UNITY_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Used to wrap AudioClip references in AudioConfigs. Allows certain extra metadata to be kept with it, such as
    /// events that are supposed to be fired at a specific time during the clip.
    /// </summary>
    [Serializable]
    public class AudioClipMetaData
    {
        [SerializeField, HideInInspector] private AudioClip audioClip;
        public AudioClip AudioClip => audioClip;
        
        [SerializeField] private List<AudioClipTimelineEvent> timelineEvents = new();
        public List<AudioClipTimelineEvent> TimelineEvents => timelineEvents;

        public static implicit operator AudioClip(AudioClipMetaData audioClipMetaData)
        {
            return audioClipMetaData.AudioClip;
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX
