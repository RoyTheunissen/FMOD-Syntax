#if FMOD_AUDIO_SYNTAX

using System;
using System.Collections.Generic;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// FMOD-specific Audio Syntax System.
    /// </summary>
    public sealed class FmodAudioSyntaxSystem
    {
        private static readonly List<IAudioPlayback> activeEventPlaybacks = new();
        public static List<IAudioPlayback> ActiveEventPlaybacks => activeEventPlaybacks;
        
        private static readonly List<FmodSnapshotPlayback> activeSnapshotPlaybacks = new();
        public static List<FmodSnapshotPlayback> ActiveSnapshotPlaybacks => activeSnapshotPlaybacks;
        
        private static readonly List<IOnFmodPlaybackRegistered> onEventPlaybackCallbackReceivers = new();
        
        [NonSerialized] private bool didInitialize;
        
        [NonSerialized] private static FmodAudioSyntaxSystem cachedInstance;
        public static FmodAudioSyntaxSystem Instance
        {
            get
            {
                if (cachedInstance == null)
                {
                    cachedInstance = new FmodAudioSyntaxSystem();
                    cachedInstance.Initialize();
                }
                return cachedInstance;
            }
        }
        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitializeOnload()
        {
            onEventPlaybackCallbackReceivers.Clear();
        }
#endif // UNITY_EDITOR
        
        public void Initialize()
        {
            if (didInitialize)
                return;

            didInitialize = true;
            
            onEventPlaybackCallbackReceivers.Clear();
        }
        
        public static void RegisterActiveEventPlayback(FmodAudioPlayback playback)
        {
            activeEventPlaybacks.Add(playback);
            
            for (int i = 0; i < onEventPlaybackCallbackReceivers.Count; i++)
            {
                onEventPlaybackCallbackReceivers[i].OnFmodPlaybackRegistered(playback);
            }
        }
        
        public static void UnregisterActiveEventPlayback(FmodAudioPlayback playback)
        {
            activeEventPlaybacks.Remove(playback);
            
            for (int i = 0; i < onEventPlaybackCallbackReceivers.Count; i++)
            {
                onEventPlaybackCallbackReceivers[i].OnFmodPlaybackUnregistered(playback);
            }
        }
        
        public static void RegisterActiveSnapshotPlayback(FmodSnapshotPlayback playback)
        {
            activeSnapshotPlaybacks.Add(playback);
        }
        
        public static void UnregisterActiveSnapshotPlayback(FmodSnapshotPlayback playback)
        {
            activeSnapshotPlaybacks.Remove(playback);
        }
        
        public static void StopAllActiveSnapshotPlaybacks()
        {
            for (int i = activeSnapshotPlaybacks.Count - 1; i >= 0; i--)
            {
                activeSnapshotPlaybacks[i].Stop();
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
#endif // FMOD_AUDIO_SYNTAX
