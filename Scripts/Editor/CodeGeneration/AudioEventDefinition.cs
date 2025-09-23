using UnityEditor;

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

        private string guid;
        public string Guid => guid;

        public AudioEventDefinition(AudioSyntaxSystems system, string path, string guid)
        {
            this.system = system;
            this.path = path;
            this.guid = guid;
        }
    }
    
#if FMOD_AUDIO_SYNTAX
    public abstract class FmodEventDefinition : AudioEventDefinition
    {
        public FmodEventDefinition(EditorEventRef eventRef)
            : base(AudioSyntaxSystems.FMOD, eventRef.GetFilteredPath(), eventRef.Guid.ToString())
        {
        }
    }
    
    public sealed class FmodAudioEventDefinition : FmodEventDefinition
    {
        public FmodAudioEventDefinition(EditorEventRef eventRef) : base(eventRef)
        {
        }
    }
    
    public sealed class FmodSnapshotEventDefinition : FmodEventDefinition
    {
        public FmodSnapshotEventDefinition(EditorEventRef eventRef) : base(eventRef)
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
                UnityAudioSyntaxSettings.GetFilteredPathForUnityAudioEventConfig(config),
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(config)))
        {
        }
    }
#endif // UNITY_AUDIO_SYNTAX
}
