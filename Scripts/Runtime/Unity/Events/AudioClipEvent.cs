using System;
using UnityEngine;

#if SCRIPTABLE_OBJECT_COLLECTION
using BrunoMikoski.ScriptableObjectCollections;
#endif // SCRIPTABLE_OBJECT_COLLECTION

namespace RoyTheunissen.FMODSyntax.UnityAudioSyntax
{
    [Serializable]
    public class AudioClipEvent
    {
        [SerializeField]
#if SCRIPTABLE_OBJECT_COLLECTION
        [SOCItemEditorOptions(ShouldDrawGotoButton = false, ShouldDrawPreviewButton = false)]
#endif // SCRIPTABLE_OBJECT_COLLECTION
        private AudioClipEventId id;
        public AudioClipEventId Id => id;

        [SerializeField] private float time;
        public float Time => time;
    }
}
