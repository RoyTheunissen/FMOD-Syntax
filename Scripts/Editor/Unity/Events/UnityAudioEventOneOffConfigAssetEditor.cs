#if UNITY_AUDIO_SYNTAX

using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Responsible for drawing a nice interface for previewing one-off events.
    /// </summary>
    [CustomEditor(typeof(UnityAudioEventOneOffConfigAsset), true)]
    public class UnityAudioEventOneOffConfigAssetEditor : UnityAudioEventConfigAssetEditor
    {
        private bool didPlayAudioClip;
        private AudioClip lastPreviewedAudioClip;

        protected override void DrawPreviewInternal(Rect position, Rect row)
        {
            base.DrawPreviewInternal(position, row);

            UnityAudioEventOneOffConfigAsset config = target as UnityAudioEventOneOffConfigAsset;

            DrawAudioPlayButton(
                ref row, true, config.AudioClips, ref lastPreviewedAudioClip, ref didPlayAudioClip, "One-Off", false);
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX
