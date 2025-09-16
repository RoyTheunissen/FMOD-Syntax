using System;
using UnityEngine;

#if SCRIPTABLE_OBJECT_COLLECTION
using BrunoMikoski.ScriptableObjectCollections;
#endif // SCRIPTABLE_OBJECT_COLLECTION

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Specifies a Timeline Event inside an Audio Clip.
    /// </summary>
    [Serializable]
    public class AudioClipTimelineEvent
    {
        [SerializeField]
#if SCRIPTABLE_OBJECT_COLLECTION
        [SOCItemEditorOptions(ShouldDrawGotoButton = false, ShouldDrawPreviewButton = false)]
#endif // SCRIPTABLE_OBJECT_COLLECTION
        private AudioTimelineEventId id;
        public AudioTimelineEventId Id => id;

        [SerializeField] private float time;
        public float Time => time;
    }
}
