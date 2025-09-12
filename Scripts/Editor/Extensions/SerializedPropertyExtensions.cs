using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Useful extension methods for EditorParamRef.
    /// </summary>
    public static class SerializedPropertyExtensions
    {
        /// <summary>
        /// From: https://gist.github.com/monry/9de7009689cbc5050c652bcaaaa11daa
        /// </summary>
        public static SerializedProperty GetParent(this SerializedProperty serializedProperty)
        {
            string[] propertyPaths = serializedProperty.propertyPath.Split('.');
            if (propertyPaths.Length <= 1)
                return default;

            SerializedProperty parentSerializedProperty =
                serializedProperty.serializedObject.FindProperty(propertyPaths.First());
            for (int index = 1; index < propertyPaths.Length - 1; index++)
            {
                if (propertyPaths[index] == "Array")
                {
                    if (index + 1 == propertyPaths.Length - 1)
                    {
                        // reached the end
                        break;
                    }
                    if (propertyPaths.Length > index + 1 && Regex.IsMatch(propertyPaths[index + 1], "^data\\[\\d+\\]$"))
                    {
                        Match match = Regex.Match(propertyPaths[index + 1], "^data\\[(\\d+)\\]$");
                        int arrayIndex = int.Parse(match.Groups[1].Value);
                        parentSerializedProperty = parentSerializedProperty.GetArrayElementAtIndex(arrayIndex);
                        index++;
                    }
                }
                else
                {
                    parentSerializedProperty = parentSerializedProperty.FindPropertyRelative(propertyPaths[index]);
                }
            }

            return parentSerializedProperty;
        }
        
        public static bool IsInArray(this SerializedProperty serializedProperty)
        {
            SerializedProperty parent = serializedProperty.GetParent();
            
            if (parent == null)
                return false;
            
            return parent.isArray;
        }
    }
}
