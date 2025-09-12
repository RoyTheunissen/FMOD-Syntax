using System;
using System.Collections.Generic;
using RoyTheunissen.FMODSyntax.Callbacks;
using RoyTheunissen.FMODSyntax.Utilities;
using UnityEngine;
using UnityEngine.Audio;
using Object = System.Object;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// FMOD-specific Audio Syntax System.
    /// </summary>
    public sealed class FmodAudioSyntaxSystem
    {
        private static readonly List<IAudioPlayback> activeEventPlaybacks = new();
        public static List<IAudioPlayback> ActiveEventPlaybacks => activeEventPlaybacks;
        
        public static FmodAudioSyntaxSystem Instance => AudioSyntaxSystem.FmodAudioSyntaxSystem;
        
        private static readonly List<IOnFmodPlaybackRegistered> onEventPlaybackCallbackReceivers = new();
        
        [NonSerialized] private bool didInitialize;
        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitializeOnload()
        {
            onEventPlaybackCallbackReceivers.Clear();
        }
#endif // UNITY_EDITOR
        public void Initialize()
        {
            if (!didInitialize)
                return;

            didInitialize = true;
            
            onEventPlaybackCallbackReceivers.Clear();
        }
        
        /// <summary>
        /// Do not call this yourself. Call AudioSyntaxSystem.RegisterActiveEventPlayback instead.
        /// Your application is preferably agnostic about the underlying audio implementation.
        /// </summary>
        public static void OnActiveEventPlaybackRegistered(FmodAudioPlayback playback)
        {
            activeEventPlaybacks.Add(playback);
            
            for (int i = 0; i < onEventPlaybackCallbackReceivers.Count; i++)
            {
                onEventPlaybackCallbackReceivers[i].OnFmodPlaybackRegistered(playback);
            }
        }
        
        /// <summary>
        /// Do not call this yourself. Call AudioSyntaxSystem.UnregisterActiveEventPlayback instead.
        /// Your application is preferably agnostic about the underlying audio implementation.
        /// </summary>
        public static void OnActiveEventPlaybackUnregistered(FmodAudioPlayback playback)
        {
            activeEventPlaybacks.Remove(playback);
            
            for (int i = 0; i < onEventPlaybackCallbackReceivers.Count; i++)
            {
                onEventPlaybackCallbackReceivers[i].OnFmodPlaybackUnregistered(playback);
            }
        }
        
        public static void RegisterEventPlaybackCallbackReceiver(IOnFmodPlaybackRegistered callbackReceiver)
        {
            onEventPlaybackCallbackReceivers.Add(callbackReceiver);
        }
        
        public static void UnregisterEventPlaybackCallbackReceiver(IOnFmodPlaybackRegistered callbackReceiver)
        {
            onEventPlaybackCallbackReceivers.Remove(callbackReceiver);
        }
    }
}
