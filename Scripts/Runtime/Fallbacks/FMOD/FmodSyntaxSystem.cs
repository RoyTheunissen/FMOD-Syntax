#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using RoyTheunissen.FMODSyntax.Callbacks;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Class to use to expose information, help manage playback instances, that sort of thing.
    /// </summary>
    [Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public static class FmodSyntaxSystem
    {
        public static List<FmodAudioPlayback> ActiveEventPlaybacks => null;
        public static List<FmodSnapshotPlayback> ActiveSnapshotPlaybacks => null;

        public static void StopAllActivePlaybacks()
        {
        }
        
        public static void RegisterActiveEventPlayback(FmodAudioPlayback playback)
        {
        }
        
        public static void UnregisterActiveEventPlayback(FmodAudioPlayback playback)
        {
        }
        
        public static void StopAllActiveEventPlaybacks()
        {
        }
        
        public static void RegisterActiveSnapshotPlayback(FmodSnapshotPlayback playback)
        {
        }
        
        public static void UnregisterActiveSnapshotPlayback(FmodSnapshotPlayback playback)
        {
        }

        /// <summary>
        /// Culls playbacks that are no longer necessary. You should perform this logic continuously.
        /// You can either call this or use the ActivePlaybacks list / playback callbacks to do it in your own system.
        /// </summary>
        public static void CullPlaybacks()
        {
        }

        public static void StopAllActiveSnapshotPlaybacks()
        {
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
        }
        
        public static void RegisterEventPlaybackCallbackReceiver(IOnFmodPlayback callbackReceiver)
        {
        }
        
        public static void UnregisterEventPlaybackCallbackReceiver(IOnFmodPlayback callbackReceiver)
        {
        }
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
