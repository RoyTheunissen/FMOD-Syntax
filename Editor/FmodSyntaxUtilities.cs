using System;
using System.Collections.Generic;
using System.IO;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Useful utility methods for the FMOD Syntax.
    /// </summary>
    public static class FmodSyntaxUtilities
    {
        private static readonly Dictionary<string, string> replacements = new Dictionary<string, string>()
        {
            { "base", "@base" },
        };
        
        private static readonly Dictionary<string, string> substitutions = new Dictionary<string, string>()
        {
            { "-", "_" },
            { " ", "" },
        };

        private static string Filter(string name, bool performReplacements = true)
        {
            if (performReplacements)
            {
                foreach (KeyValuePair<string, string> replacement in replacements)
                {
                    if (name == replacement.Key)
                        return replacement.Value;
                }
            }
            
            foreach (KeyValuePair<string,string> substitution in substitutions)
            {
                name = name.Replace(substitution.Key, substitution.Value);
            }
            
            return name;
        }
        
        public static string GetDisplayNameFromPath(string path)
        {
            return Path.GetFileName(path);
        }
        
        public static string GetFilteredNameFromPath(string path)
        {
            string name = Path.GetFileName(path).ToPascalCasing();
            name = Filter(name);
            return name;
        }
        
        public static string GetFilteredNameFromPathLowerCase(string path)
        {
            string name = GetFilteredNameFromPath(path).ToCamelCasing();
            name = Filter(name);
            return name;
        }
        
        public static string GetFilteredPath(string path, bool stripSpecialCharacters)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            
            const char separator = '/';
            
            // Events start with something like event:/ , so get rid of that first.
            path = path.Substring(path.IndexOf(separator, StringComparison.Ordinal) + 1);
            
            if (stripSpecialCharacters)
            {
                // Clean up every section a bit.
                string[] pathSections = path.Split(separator);
                for (int i = 0; i < pathSections.Length; i++)
                {
                    pathSections[i] = pathSections[i].ToPascalCasing();
                    pathSections[i] = Filter(pathSections[i], false);
                }

                path = string.Join(separator, pathSections);
            }
            
            return path;
        }
    }
}
