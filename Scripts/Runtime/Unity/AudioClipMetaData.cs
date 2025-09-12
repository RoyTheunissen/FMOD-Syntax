using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax.UnityAudioSyntax
{
    /// <summary>
    /// Used to wrap AudioClip references in AudioConfigs. Allows certain extra metadata to be kept with it, such as
    /// events that are supposed to be fired at a specific time during the clip.
    /// </summary>
    [Serializable]
    public class AudioClipMetaData
    {
        [SerializeField] private bool expand;
        public bool Expand => expand;

        [SerializeField] private AudioClip audioClip;
        public AudioClip AudioClip => audioClip;
        
        [SerializeField] private List<AudioClipEvent> events = new List<AudioClipEvent>();
        public List<AudioClipEvent> Events => events;

        public static implicit operator AudioClip(AudioClipMetaData audioClipMetaData)
        {
            return audioClipMetaData.AudioClip;
        }
    }
}
