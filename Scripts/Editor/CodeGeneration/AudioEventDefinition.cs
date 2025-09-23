using System.Collections.Generic;
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

        public abstract string PlaybackBaseType
        {
            get;
        }

        protected readonly List<AudioEventParameterDefinition> parameters = new();
        public IReadOnlyList<AudioEventParameterDefinition> Parameters => parameters;

        public bool IsParameterless => parameters.Count == 0;

        public abstract bool IsOneShot
        {
            get;
        }

        public abstract string Name
        {
            get;
        }

        protected AudioEventDefinition(AudioSyntaxSystems system, string path, string guid)
        {
            this.system = system;
            this.path = path;
            this.guid = guid;
        }
        
        public abstract string GetConfigBaseType(string eventName);
        
        public string GetFilteredPath(bool stripSpecialCharacters = false)
        {
            return FmodSyntaxUtilities.GetFilteredPath(Path, stripSpecialCharacters);
        }
    }

#if FMOD_AUDIO_SYNTAX
    public abstract class FmodEventDefinition : AudioEventDefinition
    {
        private readonly EditorEventRef eventRef;
        public EditorEventRef EventRef => eventRef;

        public override bool IsOneShot => eventRef.IsOneShot;

        public override string Name => eventRef.name;
        
        public override string PlaybackBaseType => "FmodAudioPlayback";

        protected FmodEventDefinition(EditorEventRef eventRef)
            : base(AudioSyntaxSystems.FMOD, eventRef.GetFilteredPath(), eventRef.Guid.ToString())
        {
            this.eventRef = eventRef;

            for (int i = 0; i < eventRef.Parameters.Count; i++)
            {
                parameters.Add(new FmodAudioEventParameterDefinition(eventRef.Parameters[i], eventRef));
            }
        }
        
        public override string GetConfigBaseType(string eventName) => $"FmodAudioConfig<{eventName}Playback>";
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
    public abstract class UnityAudioEventDefinition : AudioEventDefinition
    {
        public abstract UnityAudioEventConfigBase Config
        {
            get;
        }

        public override string Name => Config.Name;

        public override bool IsOneShot => Config is UnityAudioEventOneOffConfig;

        protected abstract string UnityAudioEventConfigBaseType
        {
            get;
        }
        
        public override string GetConfigBaseType(string eventName)
        {
            return $"UnityAudioEventPlayableConfig<{UnityAudioEventConfigBaseType}, {eventName}Playback>";
        }

        protected UnityAudioEventDefinition(UnityAudioEventConfigBase config)
            : base(AudioSyntaxSystems.UnityNativeAudio,
                UnityAudioSyntaxSettings.GetFilteredPathForUnityAudioEventConfig(config),
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(config)))
        {
        }
    }
    
    public sealed class UnityAudioEventOneOffDefinition : UnityAudioEventDefinition
    {
        private UnityAudioEventOneOffConfig config;
        public override UnityAudioEventConfigBase Config => config;
        
        public override string PlaybackBaseType => "UnityAudioOneOffPlayback";
        protected override string UnityAudioEventConfigBaseType => "UnityAudioEventOneOffConfig";

        public UnityAudioEventOneOffDefinition(UnityAudioEventOneOffConfig config) : base(config)
        {
            this.config = config;
        }
    }
    
    public sealed class UnityAudioEventLoopingDefinition : UnityAudioEventDefinition
    {
        private UnityAudioEventLoopingConfig config;
        public override UnityAudioEventConfigBase Config => config;
        
        public override string PlaybackBaseType => "UnityAudioLoopingPlayback";
        protected override string UnityAudioEventConfigBaseType => "UnityAudioEventLoopingConfig";

        public UnityAudioEventLoopingDefinition(UnityAudioEventLoopingConfig config) : base(config)
        {
            this.config = config;
        }
    }
#endif // UNITY_AUDIO_SYNTAX
}
