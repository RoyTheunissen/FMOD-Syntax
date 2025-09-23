#if FMOD_AUDIO_SYNTAX
using FMODUnity;
#endif // FMOD_AUDIO_SYNTAX

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Defines an audio event for the purpose of code generation.
    /// </summary>
    public abstract class AudioEventDefinition
    {
        private AudioSyntaxSystems system;
        public AudioSyntaxSystems System => system;

        private string path;
        public string Path => path;

        public AudioEventDefinition(AudioSyntaxSystems system, string path)
        {
            this.system = system;
            this.path = path;
        }
    }
    
#if FMOD_AUDIO_SYNTAX
    public abstract class FmodEventDefinition : AudioEventDefinition
    {
        public FmodEventDefinition(EditorEventRef eventRef, string filteredPath)
            : base(AudioSyntaxSystems.FMOD, filteredPath)
        {
        }
    }
    
    public sealed class FmodAudioEventDefinition : FmodEventDefinition
    {
        public FmodAudioEventDefinition(EditorEventRef eventRef, string filteredPath) : base(eventRef, filteredPath)
        {
        }
    }
    
    public sealed class FmodSnapshotEventDefinition : FmodEventDefinition
    {
        public FmodSnapshotEventDefinition(EditorEventRef eventRef, string filteredPath) : base(eventRef, filteredPath)
        {
        }
    }
#endif // FMOD_AUDIO_SYNTAX
    
#if UNITY_AUDIO_SYNTAX
    public sealed class UnityAudioEventDefinition : AudioEventDefinition
    {
        private UnityAudioEventConfigBase config;
        public UnityAudioEventConfigBase Config => config;

        public UnityAudioEventDefinition(UnityAudioEventConfigBase config)
            : base(AudioSyntaxSystems.UnityNativeAudio,
                UnityAudioSyntaxSettings.GetFilteredPathForUnityAudioEventConfig(config))
        {
        }
    }
#endif // UNITY_AUDIO_SYNTAX
}
