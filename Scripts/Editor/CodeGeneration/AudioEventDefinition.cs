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
    public abstract class FmodAudioEventDefinition : AudioEventDefinition
    {
        public FmodAudioEventDefinition(string path) : base(AudioSyntaxSystems.FMOD, path)
        {
        }
    }
#endif // FMOD_AUDIO_SYNTAX
    
#if UNITY_AUDIO_SYNTAX
    public abstract class UnityAudioEventDefinition : AudioEventDefinition
    {
        private UnityAudioEventConfigBase config;
        public UnityAudioEventConfigBase Config => config;

        public UnityAudioEventDefinition(string path) : base(AudioSyntaxSystems.UnityNativeAudio, path)
        {
        }
    }
#endif // UNITY_AUDIO_SYNTAX
}
