#if UNITY_AUDIO_SYNTAX
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Responsible for drawing a nice interface for previewing looping events.
    /// 
    /// NOTE: I would have liked to make one Play button and have it automatically play start / stop audio as-needed,
    /// which would be more representative, but it turns out that if you play multiple previews in succession,
    /// then the previous one cannot get stopped any more. So this functionality is not currently possible.
    /// </summary>
    [CustomEditor(typeof(UnityAudioEventLoopingConfigAsset), true)]
    public class UnityAudioEventLoopingConfigAssetEditor : UnityAudioEventConfigAssetEditor
    {
        private bool didPlayStartAudioClip;
        private AudioClip lastPreviewedStartAudioClip;
        
        private bool didPlayLoopAudioClip;
        private AudioClip lastPreviewedLoopingAudioClip;
        
        private bool didPlayEndAudioClip;
        private AudioClip lastPreviewedEndAudioClip;

        protected override int PreviewRowCount => 1;
        protected override float PreviewLabelWidth => 37;

        protected override void DrawPreviewInternal(Rect position, Rect row)
        {
            base.DrawPreviewInternal(position, row);

            UnityAudioEventLoopingConfigAsset config = target as UnityAudioEventLoopingConfigAsset;
            
            DrawAudioPlayButton(
                ref row,
                true, config, ref lastPreviewedLoopingAudioClip, ref didPlayLoopAudioClip, "Loop",
                isPlayingLoop, true);
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX
