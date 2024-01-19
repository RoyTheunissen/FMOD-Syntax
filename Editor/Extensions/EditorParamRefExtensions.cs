using System;
using FMODUnity;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Useful extension methods for EditorParamRef.
    /// </summary>
    public static class EditorParamRefExtensions
    {
        public static string GetFilteredName(this EditorParamRef parameter)
        {
            return FmodSyntaxUtilities.GetFilteredNameFromPath(parameter.Name);
        }
        
        public static string GetArgumentName(this EditorParamRef parameter)
        {
            return FmodSyntaxUtilities.GetFilteredNameFromPathLowerCase(parameter.Name);
        }

        public static bool HasNormalizedRange(this EditorParamRef parameter) =>
            Mathf.Approximately(parameter.Min, 0.0f) && Mathf.Approximately(parameter.Max, 1.0f);

        private static string GetLabelParameterTypeName(
            this EditorParamRef parameter, bool fullyQualified, EditorEventRef @event)
        {
            string name = parameter.GetFilteredName();
            bool hasUserEnum = FmodCodeGenerator.GetUserSpecifiedLabelParameterType(name, out Type userEnum);
            if (hasUserEnum)
                return userEnum.FullName;

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
                {
#if SCRIPTABLE_OBJECT_COLLECTION
                    string name = parameter.GetFilteredName();
                    bool hasUserType = FmodCodeGenerator.GetUserSpecifiedLabelParameterType(name, out Type userType);
                    if (hasUserType && !typeof(Enum).IsAssignableFrom(userType))
                        return $"ParameterScriptableObjectCollectionItem<{parameter.GetLabelParameterTypeName(false, null)}>";
#endif // SCRIPTABLE_OBJECT_COLLECTION
                        
                    return $"ParameterEnum<{parameter.GetLabelParameterTypeName(false, null)}>";
                }
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
                    return $"{parameter.GetLabelParameterTypeName(false, null)}";
            }
        }
        
        public static string GetArgumentTypeFullyQualified(this EditorParamRef parameter, EditorEventRef @event)
        {
            string type = parameter.GetArgumentType();
            if (parameter.Type == ParameterType.Labeled)
                type = parameter.GetLabelParameterTypeName(true, @event);
            return type;
        }
    }
}
