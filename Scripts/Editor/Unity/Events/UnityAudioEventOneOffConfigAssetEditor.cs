#if UNITY_AUDIO_SYNTAX

using UnityEditor;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Responsible for drawing a nice interface for previewing one-off events.
    /// </summary>
    [CustomEditor(typeof(UnityAudioEventOneOffConfigAsset), true)]
    public class UnityAudioEventOneOffConfigAssetEditor : UnityAudioEventConfigAssetEditor
    {
    }
}
#endif // UNITY_AUDIO_SYNTAX
