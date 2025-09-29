#if SCRIPTABLE_OBJECT_COLLECTION

using UnityEngine;
using BrunoMikoski.ScriptableObjectCollections;

namespace RoyTheunissen.AudioSyntax
{
    [CreateAssetMenu(menuName = AudioSyntaxMenuPaths.CreateSocCollection + "Create Audio Timeline Event ID Collection",
        fileName = "AudioTimelineEventIdCollection", order = 0)]
    public class AudioTimelineEventIdCollection : ScriptableObjectCollection<AudioTimelineEventId>
    {
    }
}

#endif // SCRIPTABLE_OBJECT_COLLECTION
