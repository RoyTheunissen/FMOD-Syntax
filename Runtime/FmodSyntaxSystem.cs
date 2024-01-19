using System;
using System.Collections.Generic;
using RoyTheunissen.FMODSyntax.Callbacks;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Class to use to expose information, help manage playback instances, that sort of thing.
    /// </summary>
    public static class FmodSyntaxSystem
    {
        private static readonly List<FmodAudioPlayback> activeEventPlaybacks = new List<FmodAudioPlayback>();
        public static List<FmodAudioPlayback> ActiveEventPlaybacks => activeEventPlaybacks;

        private static readonly List<IOnFmodPlayback> onEventPlaybackCallbackReceivers = new List<IOnFmodPlayback>();
        
        private static readonly List<FmodSnapshotPlayback> activeSnapshotPlaybacks = new List<FmodSnapshotPlayback>();
        public static List<FmodSnapshotPlayback> ActiveSnapshotPlaybacks => activeSnapshotPlaybacks;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            activeEventPlaybacks.Clear();
            onEventPlaybackCallbackReceivers.Clear();
        }
#endif // UNITY_EDITOR

        public static void StopAllActivePlaybacks()
        {
            StopAllActiveEventPlaybacks();
            StopAllActiveSnapshotPlaybacks();
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
        
        public static void StopAllActiveEventPlaybacks()
        {
            for (int i = activeEventPlaybacks.Count - 1; i >= 0; i--)
            {
                activeEventPlaybacks[i].Stop();
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

        /// <summary>
        /// Culls playbacks that are no longer necessary. You should perform this logic continuously.
        /// You can either call this or use the ActivePlaybacks list / playback callbacks to do it in your own system.
        /// </summary>
        public static void CullPlaybacks()
        {
            // Cull any events that are ready to be cleaned up.
            for (int i = activeEventPlaybacks.Count - 1; i >= 0; i--)
            {
                IFmodPlayback activeEvent = activeEventPlaybacks[i];
                if (activeEvent.CanBeCleanedUp)
                    activeEvent.Cleanup();
            }
            
            // Cull any snapshots that are ready to be cleaned up.
            for (int i = activeSnapshotPlaybacks.Count - 1; i >= 0; i--)
            {
                IFmodPlayback activeSnapshot = activeSnapshotPlaybacks[i];
                if (activeSnapshot.CanBeCleanedUp)
                    activeSnapshot.Cleanup();
            }
        }

        public static void StopAllActiveSnapshotPlaybacks()
        {
            for (int i = activeSnapshotPlaybacks.Count - 1; i >= 0; i--)
            {
                activeSnapshotPlaybacks[i].Stop();
            }
        }
        
        [Obsolete("This method is being renamed for disambiguation. " +
                  "Please use RegisterEventPlaybackCallbackReceiver instead.")]
        public static void RegisterPlaybackCallbackReceiver(IOnFmodPlayback callbackReceiver)
        {
            RegisterEventPlaybackCallbackReceiver(callbackReceiver);
        }
        
        [Obsolete("This method is being renamed for disambiguation. " +
                  "Please use UnregisterEventPlaybackCallbackReceiver instead.")]
        public static void UnregisterPlaybackCallbackReceiver(IOnFmodPlayback callbackReceiver)
        {
            UnregisterEventPlaybackCallbackReceiver(callbackReceiver);
        }
        
        public static void RegisterEventPlaybackCallbackReceiver(IOnFmodPlayback callbackReceiver)
        {
            onEventPlaybackCallbackReceivers.Add(callbackReceiver);
        }
        
        public static void UnregisterEventPlaybackCallbackReceiver(IOnFmodPlayback callbackReceiver)
        {
            onEventPlaybackCallbackReceivers.Remove(callbackReceiver);
        }
    }
}
