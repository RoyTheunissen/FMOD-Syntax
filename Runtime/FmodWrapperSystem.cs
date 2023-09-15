using System.Collections.Generic;
using RoyTheunissen.FMODWrapper.Runtime.Callbacks;

namespace RoyTheunissen.FMODWrapper
{
    /// <summary>
    /// Class to use to expose information, help manage playback instances, that sort of thing.
    /// </summary>
    public static class FmodWrapperSystem
    {
        private static readonly List<FmodAudioPlayback> activePlaybacks = new List<FmodAudioPlayback>();
        public static List<FmodAudioPlayback> ActivePlaybacks => activePlaybacks;

        private static readonly List<IOnFmodPlayback> onPlaybackCallbackReceivers = new List<IOnFmodPlayback>();

        public static void RegisterActivePlayback(FmodAudioPlayback playback)
        {
            activePlaybacks.Add(playback);
            
            for (int i = 0; i < onPlaybackCallbackReceivers.Count; i++)
            {
                onPlaybackCallbackReceivers[i].OnFmodPlaybackRegistered(playback);
            }
        }
        
        public static void UnregisterActivePlayback(FmodAudioPlayback playback)
        {
            activePlaybacks.Remove(playback);
            
            for (int i = 0; i < onPlaybackCallbackReceivers.Count; i++)
            {
                onPlaybackCallbackReceivers[i].OnFmodPlaybackUnregistered(playback);
            }
        }

        /// <summary>
        /// Culls playbacks that are no longer necessary. You should perform this logic continuously.
        /// You can either call this or use the ActivePlaybacks list / playback callbacks to do it in your own system.
        /// </summary>
        public static void CullPlaybacks()
        {
            for (int i = activePlaybacks.Count - 1; i >= 0; i--)
            {
                IAudioPlayback activePlayback = activePlaybacks[i];
                if (activePlayback.CanBeCleanedUp)
                    activePlayback.Cleanup();
            }
        }
        
        public static void RegisterPlaybackCallbackReceiver(IOnFmodPlayback callbackReceiver)
        {
            onPlaybackCallbackReceivers.Add(callbackReceiver);
        }
        
        public static void UnregisterPlaybackCallbackReceiver(IOnFmodPlayback callbackReceiver)
        {
            onPlaybackCallbackReceivers.Remove(callbackReceiver);
        }
    }
}
