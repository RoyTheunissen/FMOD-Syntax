#if UNITY_AUDIO_SYNTAX

using System;
using RoyTheunissen.FMODSyntax.UnityAudioSyntax;

namespace RoyTheunissen.FMODSyntax
{
    // Contains all the fields, properties and methods that are entirely Unity Audio-specific, for readability.
    public static partial class AudioSyntaxSystem
    {
        [NonSerialized] private static UnityAudioSyntaxSystem cachedUnityAudioSyntaxSystem;
        [NonSerialized] private static bool initializedUnityAudioSyntaxSystem;
        public static UnityAudioSyntaxSystem UnityAudioSyntaxSystem
        {
            get
            {
                InitializeUnityAudioSyntaxSystem();
                return cachedUnityAudioSyntaxSystem;
            }
        }

        public static void InitializeUnityAudioSyntaxSystem()
        {
            if (initializedUnityAudioSyntaxSystem)
                return;
            
            initializedUnityAudioSyntaxSystem = true;
            cachedUnityAudioSyntaxSystem = new UnityAudioSyntaxSystem();
            cachedUnityAudioSyntaxSystem.Initialize(
                AudioSyntaxSettings.Instance.AudioSourcePooledPrefab, AudioSyntaxSettings.Instance.DefaultMixerGroup);
        }
    }
}

#endif // UNITY_AUDIO_SYNTAX
