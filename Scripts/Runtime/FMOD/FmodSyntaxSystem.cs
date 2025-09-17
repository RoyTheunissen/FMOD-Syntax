#if FMOD_AUDIO_SYNTAX

using System;
using System.Collections.Generic;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// FMOD-specific Audio Syntax System.
    ///
    /// NOTE: It could be argued that the name FmodAudioSyntaxSystem would be more consistent with
    /// UnityAudioSyntaxSystem. However, for backwards compatibility I have kept the same name.
    /// Also, not that while Unity Syntax is vague and may not relate to audio, FMOD Syntax is clearly about audio.
    /// So there are other cases where we use FMOD directly but Unity Audio instead of just Unity.
    /// </summary>
    public sealed class FmodSyntaxSystem
    {
        private static readonly List<IAudioPlayback> activeEventPlaybacks = new();
        public static List<IAudioPlayback> ActiveEventPlaybacks => activeEventPlaybacks;
        
        private static readonly List<FmodSnapshotPlayback> activeSnapshotPlaybacks = new();
        public static List<FmodSnapshotPlayback> ActiveSnapshotPlaybacks => activeSnapshotPlaybacks;
        
        private static readonly List<IOnFmodPlaybackRegistered> onEventPlaybackCallbackReceivers = new();
        
        [NonSerialized] private bool didInitialize;
        
        [NonSerialized] private static FmodSyntaxSystem cachedInstance;
        public static FmodSyntaxSystem Instance
        {
            get
            {
                if (cachedInstance == null)
                {
                    cachedInstance = new FmodSyntaxSystem();
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
