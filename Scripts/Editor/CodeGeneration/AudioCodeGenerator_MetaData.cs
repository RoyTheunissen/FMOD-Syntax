using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public static partial class AudioCodeGenerator
    {
        [Serializable]
        public sealed class MetaData
        {
            [SerializeField] private string version = "0.0.0";
            public string Version => version;

            [SerializeField] private AudioSyntaxSettings.SyntaxFormats syntaxFormat;
            public AudioSyntaxSettings.SyntaxFormats SyntaxFormat => syntaxFormat;

            [SerializeField] private string[] eventGuidToPreviousSyntaxPaths = Array.Empty<string>();

            [NonSerialized] private Dictionary<string, string> cachedEventGuidToPreviousSyntaxPathDictionary = new();
            [NonSerialized] private bool didCacheEventGuidToPreviousSyntaxPathDictionary;

            public Dictionary<string, string> EventGuidToPreviousSyntaxPath
            {
                get
                {
                    if (!didCacheEventGuidToPreviousSyntaxPathDictionary)
                    {
                        didCacheEventGuidToPreviousSyntaxPathDictionary = true;
                        for (int i = 0; i < eventGuidToPreviousSyntaxPaths.Length; i += 2)
                        {
                            string key = eventGuidToPreviousSyntaxPaths[i];
                            string value = eventGuidToPreviousSyntaxPaths[i + 1];
                            cachedEventGuidToPreviousSyntaxPathDictionary.Add(key, value);
                        }
                    }

                    return cachedEventGuidToPreviousSyntaxPathDictionary;
                }
                set
                {
                    List<string> previousEventSyntaxPathValues = new();
                    foreach (KeyValuePair<string, string> stringPair in value)
                    {
                        previousEventSyntaxPathValues.Add(stringPair.Key);
                        previousEventSyntaxPathValues.Add(stringPair.Value);
                    }

                    this.eventGuidToPreviousSyntaxPaths = previousEventSyntaxPathValues.ToArray();
                }
            }

            public MetaData()
            {
            }

            public MetaData(
                string version, AudioSyntaxSettings.SyntaxFormats syntaxFormat,
                Dictionary<string, string> eventGuidToPreviousSyntaxPaths)
            {
                this.version = version;
                this.syntaxFormat = syntaxFormat;
                EventGuidToPreviousSyntaxPath = eventGuidToPreviousSyntaxPaths;
            }

            public string GetJson()
            {
                string json = JsonUtility.ToJson(this, true);
                json = CleanUpJsonDictionarySyntax(json, "eventGuidToPreviousSyntaxPaths");
                return json;
            }
        }

        private enum MetaDataFormats
        {
            None, // Could not be parsed.
            ActiveEventGuids, // The old format that just specified which events are currently being used and their name
            Json, // The new format that still stores old event data but also supports storing additional data
        }

        [NonSerialized] private static string rawMetaDataFromPreviousCodeGeneration;
        [NonSerialized] private static MetaDataFormats metaDataFormatFromPreviousCodeGeneration;
        [NonSerialized] private static MetaData metaDataFromPreviousCodeGeneration;

        private static string PreviousMetaDataFilePath
        {
            get
            {
                string userSettingsFolder = Application.dataPath.GetParentDirectory() + "/UserSettings/";
                return userSettingsFolder + "FMOD-Syntax/previousMetaData.json";
            }
        }

        private static string GetRawMetaDataFromEventsScript(out MetaDataFormats format)
        {
            format = MetaDataFormats.None;

            // If a script has already been generated, open it.
            string existingFilePath = EventsScriptPath.GetAbsolutePath();
            if (!File.Exists(existingFilePath))
                return string.Empty;

            string existingCode = File.ReadAllText(existingFilePath);

            // Check that there's a section with existing events by GUID. This is the old format for metadata.
            const string oldMetaDataStart = "/* ACTIVE EVENT GUIDS";
            const string oldMetaDataEnd = "ACTIVE EVENT GUIDS */";
            string activeEventGuidsSection = existingCode.GetSection(oldMetaDataStart, oldMetaDataEnd);
            if (!string.IsNullOrEmpty(activeEventGuidsSection))
            {
                format = MetaDataFormats.ActiveEventGuids;

                string[] lines = activeEventGuidsSection.Split("\r\n");
                List<string> linesList = lines.ToList();
                // Skip the first line because it just has some explanation for what it's for.
                if (linesList.Count > 0)
                    linesList.RemoveAt(0);
                // Also skip the last line because it's empty.
                if (linesList.Count > 0)
                    linesList.RemoveAt(linesList.Count - 1);
                activeEventGuidsSection = string.Join("\r\n", linesList);

                return activeEventGuidsSection;
            }

            // Now check the new format.
            const string newMetaDataStart =
                "/* ---------------------------------------------- METADATA ------------------------------------------------------";
            const string newMetaDataEnd =
                "------------------------------------------------- METADATA --------------------------------------------------- */";
            string metaDataSection = existingCode.GetSection(newMetaDataStart, newMetaDataEnd);
            if (!string.IsNullOrEmpty(metaDataSection))
            {
                format = MetaDataFormats.Json;
                return metaDataSection;
            }

            return string.Empty;
        }

        private static void ParseMetaData()
        {
            rawMetaDataFromPreviousCodeGeneration = GetRawMetaDataFromEventsScript(
                out metaDataFormatFromPreviousCodeGeneration);

            if (metaDataFormatFromPreviousCodeGeneration == MetaDataFormats.None)
            {
                // Metadata could not be found. Just assume the default empty metadata.
                metaDataFromPreviousCodeGeneration = new MetaData();
            }
            else if (metaDataFormatFromPreviousCodeGeneration == MetaDataFormats.ActiveEventGuids)
            {
                // The metadata was in the old format. This one did not support name different syntax format nor did
                // it specify a version number; just event GUIDs and their previous name.
                string version = "0.0.1";

                AudioSyntaxSettings.SyntaxFormats syntaxFormat = AudioSyntaxSettings.SyntaxFormats.Flat;

                Dictionary<string, string> eventGuidToPreviousSyntaxPath = GetExistingEventSyntaxPathsByGuid();

                metaDataFromPreviousCodeGeneration = new MetaData(
                    version, syntaxFormat, eventGuidToPreviousSyntaxPath);
            }
            else
            {
                // The metadata was in the new JSON format so we can just deserialize it.
                metaDataFromPreviousCodeGeneration = JsonUtility.FromJson<MetaData>(
                    rawMetaDataFromPreviousCodeGeneration);
            }
        }

        private static void LoadPreviousMetaData()
        {
            string previousMetaDataFilePath = PreviousMetaDataFilePath;
            if (File.Exists(previousMetaDataFilePath))
            {
                metaDataFormatFromPreviousCodeGeneration = MetaDataFormats.Json;
                rawMetaDataFromPreviousCodeGeneration = File.ReadAllText(previousMetaDataFilePath);
                metaDataFromPreviousCodeGeneration = JsonUtility.FromJson<MetaData>(
                    rawMetaDataFromPreviousCodeGeneration);
            }
            else
            {
                metaDataFormatFromPreviousCodeGeneration = MetaDataFormats.None;
                rawMetaDataFromPreviousCodeGeneration = null;
                metaDataFromPreviousCodeGeneration = new MetaData();
            }
        }

        private static void StorePreviousMetaData()
        {
            if (metaDataFormatFromPreviousCodeGeneration == MetaDataFormats.None)
            {
                if (File.Exists(PreviousMetaDataFilePath))
                    File.Delete(PreviousMetaDataFilePath);
                return;
            }

            string metaDataFromPreviousCodeGenerationJson = metaDataFromPreviousCodeGeneration.GetJson();

            string path = PreviousMetaDataFilePath;

            string directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);

            File.WriteAllText(path, metaDataFromPreviousCodeGenerationJson);
        }
    }
}