using System;

namespace RoyTheunissen.FMODWrapper
{
    /// <summary>
    /// Attribute to let the FMOD wrapper system know that a custom enum is used to represent a labelled parameter.
    /// This is useful if for example you have a labelled parameter that is shared between various events, and you want
    /// to have only one enum represent it instead of having to map enums back-and-forth. 
    /// </summary>
    public class FmodLabelEnumAttribute : Attribute
    {
        private string[] labelledParameterNames;
        public string[] LabelledParameterNames => labelledParameterNames;

        public FmodLabelEnumAttribute(params string[] labelledParameterNames)
        {
            this.labelledParameterNames = labelledParameterNames;
        }
    }
}
