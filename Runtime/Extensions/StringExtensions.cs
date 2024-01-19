using System;
using System.IO;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    public static class StringExtensions
    {
        private const string HungarianPrefix = "m_";
        private const char Underscore = '_';
        public const char DefaultSeparator = ' ';
        
        private static readonly char[] DirectorySeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private static bool IsExcludedSymbol(char symbol, char wordSeparator = DefaultSeparator)
        {
            return char.IsWhiteSpace(symbol) || char.IsPunctuation(symbol) || symbol == wordSeparator;
        }

        public static string ToCamelCasing(this string text, char wordSeparator = DefaultSeparator)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Split the text up into separate words first then fix spaces and change captialization.
            text = text.ToHumanReadable(wordSeparator);

            string camelText = string.Empty;
            for (int i = 0; i < text.Length; i++)
            {
                // Separators cause the next character to be capitalized.
                if (char.IsWhiteSpace(text[i]) || text[i] == wordSeparator)
                {
                    // Non-whitespace separators are allowed through.
                    if (!char.IsWhiteSpace(text[i]))
                        camelText += text[i];

                    // If there is a character after the whitespace, add that as capitalized.
                    if (i + 1 < text.Length)
                    {
                        i++;
                        camelText += char.ToUpper(text[i]);
                    }

                    continue;
                }

                camelText += char.ToLower(text[i]);
            }

            return camelText;
        }

        public static string ToPascalCasing(this string text, char wordSeparator = DefaultSeparator)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Capitalize the first letter.
            string camelCasingText = text.ToCamelCasing(wordSeparator);
            if (camelCasingText.Length == 1)
                return camelCasingText.ToUpper();

            return char.ToUpper(camelCasingText[0]) + camelCasingText.Substring(1);
        }

        /// <summary>
        /// Gets the human readable version of programmer text, like a variable name.
        /// </summary>
        /// <param name="programmerText">The programmer text.</param>
        /// <returns>The human readable equivalent of the programmer text.</returns>
        public static string ToHumanReadable(
            this string programmerText,
            char wordSeparator = DefaultSeparator)
        {
            if (string.IsNullOrEmpty(programmerText))
                return programmerText;

            bool wasLetter = false;
            bool wasUpperCase = false;
            bool addedSpace = false;
            string result = "";

            // First remove the m_ prefix if it exists.
            if (programmerText.StartsWith(HungarianPrefix))
                programmerText = programmerText.Substring(HungarianPrefix.Length);

            // Deal with any miscellanneous spaces.
            if (wordSeparator != DefaultSeparator)
                programmerText = programmerText.Replace(DefaultSeparator, wordSeparator);

            // Deal with any miscellanneous underscores.
            if (wordSeparator != Underscore)
                programmerText = programmerText.Replace(Underscore, wordSeparator);

            // Go through the original string and copy it with some modifications.
            for (int i = 0; i < programmerText.Length; i++)
            {
                // If there was a change in caps add spaces.
                if ((wasUpperCase != char.IsUpper(programmerText[i])
                     || (wasLetter != char.IsLetter(programmerText[i])))
                    && i > 0 && !addedSpace
                    && !(IsExcludedSymbol(programmerText[i], wordSeparator) ||
                         IsExcludedSymbol(programmerText[i - 1], wordSeparator)))
                {
                    // Upper case to lower case.
                    // I added this so that something like 'GUIItem' turns into 'GUI Item', but that 
                    // means we have to make sure that no symbols are involved. Also check that there 
                    // isn't already a space where we want to add a space. Don't want to double space.
                    if (wasUpperCase && i > 1 && !IsExcludedSymbol(programmerText[i - 1], wordSeparator)
                        && !IsExcludedSymbol(result[result.Length - 2], wordSeparator))
                    {
                        // From letter to letter means we have to insert a space one character back.
                        // Otherwise it's going from a letter to a symbol and we can just add a space.
                        if (wasLetter && char.IsLetter(programmerText[i]))
                            result = result.Insert(result.Length - 1, wordSeparator.ToString());
                        else
                            result += wordSeparator;
                        addedSpace = true;
                    }

                    // Lower case to upper case.
                    if (!wasUpperCase)
                    {
                        result += wordSeparator;
                        addedSpace = true;
                    }
                }
                else
                {
                    // No case change.
                    addedSpace = false;
                }

                // Add the character.
                result += programmerText[i];

                // Capitalize the first character.
                if (i == 0)
                    result = result.ToUpper();

                // Remember things about the previous letter.
                wasLetter = char.IsLetter(programmerText[i]);
                wasUpperCase = char.IsUpper(programmerText[i]);
            }

            return result;
        }

        public static string RemovePrefix(this string name, string prefix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(prefix))
                return name;

            if (!name.StartsWith(prefix))
                return name;

            return name.Substring(prefix.Length);
        }

        public static string RemoveSuffix(this string name, string suffix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(suffix))
                return name;

            if (!name.EndsWith(suffix))
                return name;

            return name.Substring(0, name.Length - suffix.Length);
        }

        /// <summary>
        /// Converts the slashes to be consistent.
        /// </summary>
        public static string ToUnityPath(this string name)
        {
            return name.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private const string AssetsFolder = "Assets";

        public static string RemoveAssetsPrefix(this string path)
        {
            return path.RemovePrefix(AssetsFolder + Path.AltDirectorySeparatorChar);
        }

        public static string GetAbsolutePath(this string projectPath)
        {
            string absolutePath = projectPath.ToUnityPath().RemoveAssetsPrefix();
            return Application.dataPath + Path.AltDirectorySeparatorChar + absolutePath;
        }

        public static string GetWhitespacePreceding(this string text, int index, bool includingNewLines)
        {
            string whitespacePreceding = string.Empty;
            if (index > 0)
            {
                for (int i = index - 1; i >= 0; i--)
                {
                    char c = text[i];
                    if (c == '\n' && !includingNewLines)
                        break;
                    if (char.IsWhiteSpace(c))
                        whitespacePreceding = text[i] + whitespacePreceding;
                    else
                        break;
                }
            }

            return whitespacePreceding;
        }

        public static string GetSection(this string text, string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                return string.Empty;

            int fromIndex = text.IndexOf(from, StringComparison.Ordinal);
            if (fromIndex == -1)
                return string.Empty;
            fromIndex += from.Length;

            int toIndex = text.IndexOf(to, fromIndex, StringComparison.Ordinal);
            if (toIndex == -1)
                return string.Empty;

            return text.Substring(fromIndex, toIndex - fromIndex);
        }
        
        public static string GetParentDirectory(this string path)
        {
            int lastDirectorySeparator = path.LastIndexOfAny(DirectorySeparators);
            if (lastDirectorySeparator == -1)
                return path;

            return path.Substring(0, lastDirectorySeparator);
        }
    }
}
