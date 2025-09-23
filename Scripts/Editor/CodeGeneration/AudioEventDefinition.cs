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
        
        private readonly string displayName;
        public string DisplayName => displayName;
        
        private readonly string filteredName;
        public string FilteredName => filteredName;
        
        private readonly string fieldName;
        public string FieldName => fieldName;

        public AudioEventDefinition(AudioSyntaxSystems system, string path, string name, string guid)
        {
            this.system = system;
            this.path = path;
            this.guid = guid;
            
            displayName = FmodSyntaxUtilities.GetDisplayNameFromPath(name);
            filteredName = FmodSyntaxUtilities.GetFilteredNameFromPath(name);
            fieldName = FmodSyntaxUtilities.GetFilteredNameFromPathLowerCase(name);
        }
        
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

        public FmodEventDefinition(EditorEventRef eventRef)
            : base(AudioSyntaxSystems.FMOD, eventRef.name, eventRef.GetFilteredPath(), eventRef.Guid.ToString())
        {
            this.eventRef = eventRef;

            for (int i = 0; i < eventRef.Parameters.Count; i++)
            {
                parameters.Add(new FmodAudioEventParameterDefinition(eventRef.Parameters[i], eventRef));
            }
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

        public override string Name => Config.Name;

        public override bool IsOneShot => Config is UnityAudioEventOneOffConfig;

        public UnityAudioEventDefinition(UnityAudioEventConfigBase config)
            : base(AudioSyntaxSystems.UnityNativeAudio,
                UnityAudioSyntaxSettings.GetFilteredPathForUnityAudioEventConfig(config), config.name,
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(config)))
        {
        }
    }
#endif // UNITY_AUDIO_SYNTAX
}
