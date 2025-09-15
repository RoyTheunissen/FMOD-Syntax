#if SCRIPTABLE_OBJECT_COLLECTION

using UnityEngine;
using BrunoMikoski.ScriptableObjectCollections;

namespace RoyTheunissen.FMODSyntax.UnityAudioSyntax
{
    [CreateAssetMenu(menuName = MenuPaths.CreateSocItem + "Create Audio Clip Event ID Collection", fileName = "AudioClipEventIdCollection", order = 0)]
    public class AudioClipEventIdCollection : ScriptableObjectCollection<AudioClipEventId>
    {
    }
}

#endif // SCRIPTABLE_OBJECT_COLLECTION
