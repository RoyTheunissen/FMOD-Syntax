using FMODUnity;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Defines a parameter for an audio event for the purpose of code generation.
    /// </summary>
    public abstract class AudioEventParameterDefinition
    {
        public abstract string Name
        {
            get;
        }
        
        public abstract string FilteredName
        {
            get;
        }
        
        public abstract string ArgumentName
        {
            get;
        }
        
        public abstract string ArgumentType
        {
            get;
        }
        
        public abstract string ArgumentTypeFullyQualified
        {
            get;
        }

        public abstract string WrapperType
        {
            get;
        }
        
        public abstract bool IsLabeled
        {
            get;
        }

        public abstract string[] Labels
        {
            get;
        }
        
        public abstract ulong ID1
        {
            get;
        }
        public abstract ulong ID2
        {
            get;
        }
    }
    
#if FMOD_AUDIO_SYNTAX
    /// <summary>
    /// FMOD-specific definition of an audio event parameter for the purpose of code generation.
    /// </summary>
    public sealed class FmodAudioEventParameterDefinition : AudioEventParameterDefinition
    {
        private readonly EditorParamRef paramRef;
        public EditorParamRef ParamRef => paramRef;

        private readonly string name;
        public override string Name => name;
        
        private readonly string filteredName;
        public override string FilteredName => filteredName;

        private readonly string argumentName;
        public override string ArgumentName => argumentName;

        private readonly string argumentType;
        public override string ArgumentType => argumentType;

        private readonly string argumentTypeFullyQualified;
        public override string ArgumentTypeFullyQualified => argumentTypeFullyQualified;

        private readonly string wrapperType;
        public override string WrapperType => wrapperType;

        public override bool IsLabeled => ParamRef.Type == ParameterType.Labeled;
        
        private readonly string[] labels;
        public override string[] Labels => labels;

        public override ulong ID1 => paramRef.ID.data1;
        public override ulong ID2 => paramRef.ID.data2;

        public FmodAudioEventParameterDefinition(EditorParamRef paramRef, EditorEventRef eventRef)
        {
            this.paramRef = paramRef;
            
            name = paramRef.Name;
            filteredName = paramRef.GetFilteredName();
            argumentName = paramRef.GetArgumentName();
            argumentType = paramRef.GetArgumentType();
            argumentTypeFullyQualified = paramRef.GetArgumentTypeFullyQualified(eventRef);

            wrapperType = paramRef.GetWrapperType();

            labels = paramRef.Labels;
        }
    }
#endif // FMOD_AUDIO_SYNTAX
    
    // NOTE: Unity Audio Syntax does not currently support parameters, but it would be nice to add in the future.
}
