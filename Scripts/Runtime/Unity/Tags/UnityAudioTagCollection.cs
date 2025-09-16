#if UNITY_AUDIO_SYNTAX

#if SCRIPTABLE_OBJECT_COLLECTION

using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax.UnityAudioSyntax.Tags
{
    [CreateAssetMenu(menuName = AudioSyntaxMenuPaths.CreateSocItem + "Create Unity Audio Tag Collection", fileName = "UnityAudioTagCollection", order = 0)]
    public class UnityAudioTagCollection : ScriptableObjectCollection<UnityAudioTag>
    {
    }
}

#endif // SCRIPTABLE_OBJECT_COLLECTION

#endif // UNITY_AUDIO_SYNTAX
