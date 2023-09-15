using System;
using FMODUnity;
using UnityEngine;

namespace RoyTheunissen.FMODWrapper
{
    /// <summary>
    /// Useful extension methods for EditorParamRef.
    /// </summary>
    public static class EditorParamRefExtensions
    {
        public static string GetFilteredName(this EditorParamRef parameter)
        {
            return FmodWrapperUtilities.GetFilteredNameFromPath(parameter.Name);
        }
        
        public static string GetArgumentName(this EditorParamRef parameter)
        {
            return FmodWrapperUtilities.GetFilteredNameFromPathLowerCase(parameter.Name);
        }

        public static bool HasNormalizedRange(this EditorParamRef parameter) =>
            Mathf.Approximately(parameter.Min, 0.0f) && Mathf.Approximately(parameter.Max, 1.0f);

        private static string GetEnumName(this EditorParamRef parameter, bool fullyQualified, EditorEventRef @event)
        {
            string name = parameter.GetFilteredName();
            bool hasUserEnum = FmodCodeGenerator.GetUserSpecifiedLabelParameterEnum(name, out Type userEnum);
            if (hasUserEnum)
                return userEnum.Name;

            string type = $"{name}Values";
            
            if (fullyQualified)
                type = $"{@event.GetFilteredName()}Playback." + type;

            return type;
        }
        
        public static string GetWrapperType(this EditorParamRef parameter)
        {
            switch (parameter.Type)
            {
                default:
                    return "ParameterFloat";
                case ParameterType.Discrete:
                    return parameter.HasNormalizedRange() ? "ParameterBool" : "ParameterInt";
                case ParameterType.Labeled:
                    return $"ParameterEnum<{parameter.GetEnumName(false, null)}>";
            }
        }
        
        public static string GetArgumentType(this EditorParamRef parameter)
        {
            switch (parameter.Type)
            {
                default:
                    return "float";
                case ParameterType.Discrete:
                    return parameter.HasNormalizedRange() ? "bool" : "int";
                case ParameterType.Labeled:
                    return $"{parameter.GetEnumName(false, null)}";
            }
        }
        
        public static string GetArgumentTypeFullyQualified(this EditorParamRef parameter, EditorEventRef @event)
        {
            string type = parameter.GetArgumentType();
            if (parameter.Type == ParameterType.Labeled)
                type = parameter.GetEnumName(true, @event);
            return type;
        }
    }
}
