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

        private bool isPreviewingLoop;
        protected override bool IsPlayingPreview => isPreviewingLoop;

        protected override void PlayPreview()
        {
            base.PlayPreview();

            isPreviewingLoop = true;
        }

        protected override void StopAllPreviews()
        {
            base.StopAllPreviews();

            isPreviewingLoop = false;
        }
    }
}
#endif // UNITY_AUDIO_SYNTAX
