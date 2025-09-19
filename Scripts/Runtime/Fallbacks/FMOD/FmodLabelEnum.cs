#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using System;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Attribute to let the FMOD Syntax system know that a custom type is used to represent a labelled parameter.
    /// This is useful if for example you have a labelled parameter that is shared between various events, and you want
    /// to have only one enum represent it instead of having to map enums back-and-forth. Instead of an enum you can now
    /// also use a Scriptable Object Collection item.
    /// </summary>
    [Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public class FmodLabelTypeAttribute : Attribute
    {
        public string[] LabelledParameterNames => Array.Empty<string>();

        public FmodLabelTypeAttribute(params string[] labelledParameterNames)
        {
        }
    }
    
    /// <summary>
    /// Basically here for backwards compatibility. FmodLabelTypeAttribute is a better name because these days it also
    /// supports linking Scriptable Object Collection item types to an FMOD Label parameter. I find it a bit harsh
    /// to mark this one as obsolete though, seems harmless to make this one just redirect to FmodLabelTypeAttribute.
    /// </summary>
    [Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public sealed class FmodLabelEnumAttribute : FmodLabelTypeAttribute
    {
        public FmodLabelEnumAttribute(params string[] labelledParameterNames)
            : base(labelledParameterNames)
        {
        }
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
