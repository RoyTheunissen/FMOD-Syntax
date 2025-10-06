using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Class to use to expose information, help manage playback instances, that sort of thing.
    /// </summary>
    public static partial class AudioSyntaxSystem
    {
        private static readonly List<IAudioPlayback> activeEventPlaybacks = new List<IAudioPlayback>();
        public static List<IAudioPlayback> ActiveEventPlaybacks => activeEventPlaybacks;

        private static readonly List<IOnAudioPlaybackRegistration> onEventPlaybackCallbackReceivers = new();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EditorInitializeOnload()
        {
            ClearAllPlaybacks();

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode || change == PlayModeStateChange.ExitingPlayMode)
                ClearAllPlaybacks();
        }
#endif // UNITY_EDITOR

        public static void StopAllActivePlaybacks()
        {
            StopAllActiveEventPlaybacks();
            
#if FMOD_AUDIO_SYNTAX
            FmodSyntaxSystem.StopAllActiveSnapshotPlaybacks();
#endif // FMOD_AUDIO_SYNTAX
        }

        public static void ClearAllPlaybacks()
        {
            StopAllActivePlaybacks();
            
            activeEventPlaybacks.Clear();
            onEventPlaybackCallbackReceivers.Clear();
            
#if UNITY_AUDIO_SYNTAX
            UnityAudioSyntaxSystem.ClearAllPlaybacks();
#endif // UNITY_AUDIO_SYNTAX
        }
        
        public static void RegisterActiveEventPlayback(IAudioPlayback playback)
        {
            activeEventPlaybacks.Add(playback);
            
            for (int i = 0; i < onEventPlaybackCallbackReceivers.Count; i++)
            {
                onEventPlaybackCallbackReceivers[i].OnAudioPlaybackRegistered(playback);
            }
            
#if FMOD_AUDIO_SYNTAX
            if (playback is FmodAudioPlayback fmodAudioPlayback)
                FmodSyntaxSystem.OnActiveEventPlaybackRegistered(fmodAudioPlayback);
#endif // FMOD_AUDIO_SYNTAX
            
#if UNITY_AUDIO_SYNTAX
            if (playback is UnityAudioPlayback unityAudioPlayback)
                UnityAudioSyntaxSystem.OnActiveEventPlaybackRegistered(unityAudioPlayback);
#endif // UNITY_AUDIO_SYNTAX
        }
        
        public static void UnregisterActiveEventPlayback(IAudioPlayback playback)
        {
            activeEventPlaybacks.Remove(playback);
            
            for (int i = 0; i < onEventPlaybackCallbackReceivers.Count; i++)
            {
                onEventPlaybackCallbackReceivers[i].OnAudioPlaybackUnregistered(playback);
            }
            
#if FMOD_AUDIO_SYNTAX
            if (playback is FmodAudioPlayback fmodAudioPlayback)
                FmodSyntaxSystem.OnActiveEventPlaybackUnregistered(fmodAudioPlayback);
#endif // FMOD_AUDIO_SYNTAX
#if UNITY_AUDIO_SYNTAX
            if (playback is UnityAudioPlayback unityAudioPlayback)
                UnityAudioSyntaxSystem.OnActiveEventPlaybackUnregistered(unityAudioPlayback);
#endif // UNITY_AUDIO_SYNTAX
        }
        
        public static void StopAllActiveEventPlaybacks()
        {
            for (int i = activeEventPlaybacks.Count - 1; i >= 0; i--)
            {
                activeEventPlaybacks[i].Stop();
            }
        }

        /// <summary>
        /// It used to be required for users to call this every frame from somewhere in their game. Now that we wish to
        /// do other functionality there as well, it has been renamed to Update. You should be calling that instead.
        /// </summary>
        [Obsolete("This method is obsolete. Please call AudioSyntaxSystem.Update instead which will also cull playbacks.")]
        public static void CullPlaybacks()
        {
            Update();
        }

        /// <summary>
        /// Call this every frame from your game and it will update the system as necessary.
        /// </summary>
        public static void Update()
        {
            CullPlaybacksInternal();
            
#if UNITY_AUDIO_SYNTAX
            UnityAudioSyntaxSystem.Instance.Update();
#endif // UNITY_AUDIO_SYNTAX
        }
        
        private static void CullPlaybacksInternal()
        {
            // Cull any events that are ready to be cleaned up.
            for (int i = activeEventPlaybacks.Count - 1; i >= 0; i--)
            {
                IAudioPlayback activeEvent = activeEventPlaybacks[i];
                if (activeEvent.CanBeCleanedUp)
                    activeEvent.Cleanup();
            }
            
#if FMOD_AUDIO_SYNTAX
            // Cull any snapshots that are ready to be cleaned up.
            for (int i = FmodSyntaxSystem.ActiveSnapshotPlaybacks.Count - 1; i >= 0; i--)
            {
                IFmodPlayback activeSnapshot = FmodSyntaxSystem.ActiveSnapshotPlaybacks[i];
                if (activeSnapshot.CanBeCleanedUp)
                    activeSnapshot.Cleanup();
            }
#endif // FMOD_AUDIO_SYNTAX
        }
        
        [Obsolete("This method is being renamed for disambiguation. " +
                  "Please use RegisterEventPlaybackCallbackReceiver instead.")]
        public static void RegisterPlaybackCallbackReceiver(IOnAudioPlaybackRegistration callbackReceiver)
        {
            RegisterEventPlaybackCallbackReceiver(callbackReceiver);
        }
        
        [Obsolete("This method is being renamed for disambiguation. " +
                  "Please use UnregisterEventPlaybackCallbackReceiver instead.")]
        public static void UnregisterPlaybackCallbackReceiver(IOnAudioPlaybackRegistration callbackReceiver)
        {
            UnregisterEventPlaybackCallbackReceiver(callbackReceiver);
        }
        
        public static void RegisterEventPlaybackCallbackReceiver(IOnAudioPlaybackRegistration callbackReceiver)
        {
            onEventPlaybackCallbackReceivers.Add(callbackReceiver);
        }
        
        public static void UnregisterEventPlaybackCallbackReceiver(IOnAudioPlaybackRegistration callbackReceiver)
        {
            onEventPlaybackCallbackReceivers.Remove(callbackReceiver);
        }
    }
}
