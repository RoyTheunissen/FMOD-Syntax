#if FMOD_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using RoyTheunissen.FMODSyntax.Callbacks;
using RoyTheunissen.FMODSyntax;

namespace RoyTheunissen.FMODSyntax
{
    // Contains all the fields, properties and methods that are entirely FMOD Audio-specific, for readability.
    public static partial class AudioSyntaxSystem
    {
        private static readonly List<FmodSnapshotPlayback> activeSnapshotPlaybacks = new List<FmodSnapshotPlayback>();
        public static List<FmodSnapshotPlayback> ActiveSnapshotPlaybacks => activeSnapshotPlaybacks;

        [NonSerialized] private static FmodAudioSyntaxSystem cachedFmodAudioSyntaxSystem;
        [NonSerialized] private static bool initializedFmodAudioSyntaxSystem;
        public static FmodAudioSyntaxSystem FmodAudioSyntaxSystem
        {
            get
            {
                InitializeFmodAudioSyntaxSystem();
                return cachedFmodAudioSyntaxSystem;
            }
        }

        public static void InitializeFmodAudioSyntaxSystem()
        {
            if (initializedFmodAudioSyntaxSystem)
                return;
            
            initializedFmodAudioSyntaxSystem = true;
            cachedFmodAudioSyntaxSystem = new FmodAudioSyntaxSystem();
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
    }
}
#endif // FMOD_AUDIO_SYNTAX
