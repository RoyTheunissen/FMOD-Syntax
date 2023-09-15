using System;
using System.IO;
using UnityEngine;

namespace RoyTheunissen.FMODWrapper
{
    public static class StringExtensions
    {
        private const string HungarianPrefix = "m_";
        private const char Underscore = '_';
        public const char DefaultSeparator = ' ';

        // Courtesy of 'ruffin' from Stack Overflow: https://pastebin.com/w6aPDn3x
        public static int IndexOfNth(this string text, string value, int occurrenceIndex)
        {
            int result = -1;

            int count = 0;
            int n = 0;

            while ((n = text.IndexOf(value, n)) != -1 && count < occurrenceIndex)
            {
                n++;
                count++;
            }

            if (count == occurrenceIndex)
                result = n;

            return result;
        }

        public static int Count(this string text, string value)
        {
            return (text.Length - text.Replace(value, "").Length) / value.Length;
        }

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

        public static string ToScreamCasing(this string text, char wordSeparator = Underscore)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Split it up into human readable words first, then simplify the casing and separation.
            return text.ToHumanReadable(wordSeparator).ToUpper();
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

        /// <summary>
        /// Splits up a string like Footstep_02 into Footstep_ (root) and 02 (numberSuffix).
        /// </summary>
        public static void GetNumberSuffix(this string name, out string root, out string numberSuffix)
        {
            GetNumberSuffix(name, false, out root, out numberSuffix);
        }

        /// <summary>
        /// Splits up a string like Footstep_02 into Footstep_ (root) and 02 (numberSuffix).
        /// </summary>
        public static void GetNumberSuffix(
            this string name, bool includeSeparatorsInNumber, out string root, out string numberSuffix)
        {
            numberSuffix = string.Empty;

            if (string.IsNullOrEmpty(name))
            {
                root = string.Empty;
                return;
            }

            for (int i = name.Length - 1; i >= 0; i--)
            {
                if (char.IsNumber(name[i]) || includeSeparatorsInNumber && (name[i] == ' ' || name[i] == '_'))
                {
                    numberSuffix = name[i] + numberSuffix;
                    continue;
                }

                break;
            }

            root = name.Substring(0, name.Length - numberSuffix.Length);
        }

        /// <summary>
        /// Splits up a string with parentheses like Footstep (New) into Footstep (root) and (New) (parentheses).
        /// </summary>
        public static void GetParenthesesSuffix(this string name, out string root, out string parenthesesSuffix)
        {
            root = name;
            parenthesesSuffix = string.Empty;

            if (string.IsNullOrEmpty(name))
                return;

            if (!name.EndsWith(")"))
                return;

            int parenthesesStart = name.LastIndexOf("(");
            root = name.Substring(0, parenthesesStart);
            parenthesesSuffix = name.Substring(parenthesesStart);
        }

        public static string RemovePrefix(this string name, string prefix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(prefix))
                return name;

            if (!name.StartsWith(prefix))
                return name;

            return name.Substring(prefix.Length);
        }

        public static string RemovePrefix(this string name, char prefix)
        {
            return name.RemovePrefix(prefix.ToString());
        }

        public static string ChangePrefix(this string name, string from, string to)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(from))
                return name;

            if (!name.StartsWith(from))
                return name;

            return to + name.Substring(from.Length);
        }

        public static string RemoveSuffix(this string name, string suffix)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(suffix))
                return name;

            if (!name.EndsWith(suffix))
                return name;

            return name.Substring(0, name.Length - suffix.Length);
        }

        public static string RemoveSuffix(this string name, char suffix)
        {
            return name.RemoveSuffix(suffix.ToString());
        }

        public static string ChangeSuffix(this string name, string from, string to)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(from))
                return name;

            if (!name.EndsWith(from))
                return name;

            return name.Substring(0, name.Length - from.Length) + to;
        }

        public static bool TryGetNumberSuffix(this string name, out int number)
        {
            number = -1;

            if (!name.EndsWith(')'))
                return false;

            int openingParenthesis = name.LastIndexOf('(');
            if (openingParenthesis == -1)
                return false;

            string suffix = name.Substring(openingParenthesis + 1);
            suffix = suffix.Substring(0, suffix.Length - 1);

            bool hasValidNumber = int.TryParse(suffix, out int parsedNumber);
            if (!hasValidNumber)
                return false;

            number = parsedNumber;
            return true;
        }

        public static string SetNumberSuffix(this string name, int number)
        {
            if (!name.EndsWith(')'))
                return $"{name} ({number})";

            int openingParenthesis = name.LastIndexOf('(');
            if (openingParenthesis == -1)
            {
                // Remove the closing parenthesis and any trailing spaces. 
                name = name.Substring(0, name.Length - 1);
                name = name.TrimEnd(' ');
                return $"{name} ({number})";
            }

            return $"{name.Substring(0, openingParenthesis)}({number.ToString()})";
        }

        public static void GetUnderscoreSuffix(this string name, out string root, out string underscoreSuffix)
        {
            root = name;
            underscoreSuffix = string.Empty;

            if (string.IsNullOrEmpty(name))
                return;

            int suffixStart = name.LastIndexOf("_");

            if (suffixStart == -1)
                return;

            root = name.Substring(0, suffixStart);
            underscoreSuffix = name.Substring(suffixStart);
        }

        public static void GetSpaceSuffix(this string name, out string root, out string spaceSuffix)
        {
            root = name;
            spaceSuffix = string.Empty;

            if (string.IsNullOrEmpty(name))
                return;

            int suffixStart = name.LastIndexOf(" ");

            if (suffixStart == -1)
                return;

            root = name.Substring(0, suffixStart);
            spaceSuffix = name.Substring(suffixStart);
        }

        /// <summary>
        /// Converts the slashes to be consistent.
        /// </summary>
        public static string ToUnityPath(this string name)
        {
            return name.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static readonly char[] DirectorySeparators =
            {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};

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

        public static string GetProjectPath(this string absolutePath)
        {
            string projectPath = Application.dataPath.RemoveSuffix(AssetsFolder)
                .RemoveSuffix(Path.AltDirectorySeparatorChar);
            string relativePath = Path.GetRelativePath(projectPath, absolutePath).ToUnityPath();
            return relativePath;
        }

        public static bool HasParentDirectory(this string path)
        {
            return path.LastIndexOfAny(DirectorySeparators) != -1;
        }

        public static string GetParentDirectory(this string path)
        {
            int lastDirectorySeparator = path.LastIndexOfAny(DirectorySeparators);
            if (lastDirectorySeparator == -1)
                return path;

            return path.Substring(0, lastDirectorySeparator);
        }

        public static bool StartsWithAny(this string path, params string[] prefixes)
        {
            foreach (string prefix in prefixes)
            {
                if (path.StartsWith(prefix))
                    return true;
            }

            return false;
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

        public static string GetWhitespaceSucceeding(this string text, int index, bool includingNewLines)
        {
            string whitespaceSucceeding = string.Empty;
            if (index > 0)
            {
                for (int i = index; i < text.Length; i++)
                {
                    char c = text[i];
                    if ((c == '\n' || c == '\r') && !includingNewLines)
                        break;
                    if (char.IsWhiteSpace(c))
                        whitespaceSucceeding = text[i] + whitespaceSucceeding;
                    else
                        break;
                }
            }

            return whitespaceSucceeding;
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
    }
}
