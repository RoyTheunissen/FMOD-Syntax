using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Utility for generating code from a template .txt file.
    /// </summary>
    public sealed class CodeGenerator 
    {
        private const string TemplateSuffix = ".txt";
        
        private const string KeywordStart = "##";
        private const string KeywordEnd = "##";

        private const string Nl = "\r\n";

        private string defaultPath;
        private string contents;

        private readonly EditorAssetReference<TextAsset> textAsset;

        private bool isInitialized;

        public CodeGenerator(string templateFileName)
        {
            textAsset = new EditorAssetReference<TextAsset>(templateFileName);
            
            // By default, just write to the folder where the template is.
            defaultPath = templateFileName.RemoveSuffix(TemplateSuffix);
        }

        private void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;
            
            Assert.IsNotNull(textAsset.Asset, $"Tried to load code template '{textAsset.Path}' which didn't exist.");

            Reset();
        }

        public void Reset()
        {
            contents = textAsset.Asset.text;
        }

        private string GetKeywordFormatted(string keyword)
        {
            return KeywordStart + keyword + KeywordEnd;
        }

        public void ReplaceKeyword(string keyword, string code, bool removeLineIfCodeIsEmpty = false)
        {
            Initialize();
            
            if (string.IsNullOrEmpty(keyword))
                return;

            bool isCodeEmpty = string.IsNullOrEmpty(code);
            
            if (isCodeEmpty && removeLineIfCodeIsEmpty)
            {
                RemoveKeywordLines(keyword);
                return;
            }
            
            keyword = GetKeywordFormatted(keyword);

            if (isCodeEmpty)
            {
                contents = contents.Replace(keyword, code);
                return;
            }

            // Make indentation consistent...
            code = code.Replace("\t", "    ");

            int indexOfKeyword = contents.IndexOf(keyword, StringComparison.Ordinal);
            while (indexOfKeyword != -1)
            {
                // Determine the whitespace immediately preceding the keyword.
                string whitespacePreceding = contents.GetWhitespacePreceding(indexOfKeyword, false);

                // Remove the keyword.
                contents = contents.Remove(indexOfKeyword, keyword.Length);
                
                // Now apply the correct indentation to every line of the code.
                string codeWithIndentation = string.Empty;
                string[] codeLines = code.TrimEnd().Split("\n");
                for (int i = 0; i < codeLines.Length; i++)
                {
                    // The first line doesn't need extra indentation because it already has it.
                    if (i > 0)
                        codeWithIndentation += whitespacePreceding;
                    codeWithIndentation += codeLines[i];
                    
                    if (i < codeLines.Length - 1)
                        codeWithIndentation += "\n";
                }

                // Now add the correctly formatted code at the specified position.
                contents = contents.Insert(indexOfKeyword, codeWithIndentation);

                // See if there's another keyword that should be replaced.
                indexOfKeyword = contents.IndexOf(keyword, StringComparison.Ordinal);
            }
        }

        public void RemoveKeywordLines(string keyword)
        {
            Initialize();
            
            if (string.IsNullOrEmpty(keyword))
                return;
            
            keyword = GetKeywordFormatted(keyword);

            List<string> lines = new List<string>(contents.Split("\n"));
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Contains(keyword))
                    lines.RemoveAt(i);
            }

            contents = string.Join("\n", lines);
        }

        public string GetCode()
        {
            Initialize();
            
            return contents;
        }

        public void GenerateFile(string path = null)
        {
            Initialize();
            
            string pathAbsolute =
                string.IsNullOrEmpty(path) ? defaultPath.GetAbsolutePath() : path.GetAbsolutePath();
            
            if (!File.Exists(pathAbsolute))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pathAbsolute));
                StreamWriter writeStream = File.CreateText(pathAbsolute);
                writeStream.Write(GetCode());
                writeStream.Flush();
                writeStream.Close();
            }
            else
                File.WriteAllText(pathAbsolute, GetCode());
            
            AssetDatabase.Refresh();
        }
    }
}
