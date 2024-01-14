using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Linq;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Generates code for FMOD events & parameters.
    /// </summary>
    public static class FmodCodeGenerator
    {
        private sealed class EventFolder
        {
            private readonly string name;
            public string Name => name;

            private readonly List<EventFolder> childFolders = new List<EventFolder>();
            public List<EventFolder> ChildFolders => childFolders;

            private readonly List<EditorEventRef> childEvents = new List<EditorEventRef>();
            public List<EditorEventRef> ChildEvents => childEvents;

            private Dictionary<EditorEventRef, string> childEventToAliasPath = new Dictionary<EditorEventRef, string>();
            public Dictionary<EditorEventRef, string> ChildEventToAliasPath => childEventToAliasPath;

            public EventFolder(string name)
            {
                this.name = name;
            }

            private EventFolder GetOrCreateSubfolder(string name)
            {
                EventFolder existingFolder = 
                    childFolders.FirstOrDefault(folder => string.Equals(folder.Name, name, StringComparison.Ordinal));
                
                if (existingFolder != null)
                    return existingFolder;
                
                EventFolder newFolder = new EventFolder(name);
                childFolders.Add(newFolder);
                return newFolder;
            }

            public EventFolder GetOrCreateChildFolderFromPathRecursively(string path)
            {
                string[] pathSections = path.Split("/");
                EventFolder currentFolder = this;
                
                // NOTE: The last section of the path is the event name.
                for (int i = 0; i < pathSections.Length - 1; i++)
                {
                    currentFolder = currentFolder.GetOrCreateSubfolder(pathSections[i]);
                }

                return currentFolder;
            }

            public override string ToString()
            {
                return $"{nameof(EventFolder)}({Name})";
            }
        }
        
        [Serializable]
        public sealed class MetaData
        {
            [SerializeField] private string version = "0.0.0";
            public string Version => version;

            [SerializeField] private FmodSyntaxSettings.EventNameClashPreventionTypes clashPreventionType;
            public FmodSyntaxSettings.EventNameClashPreventionTypes ClashPreventionType => clashPreventionType;

            [SerializeField] private string[] eventGuidToPreviousSyntaxPaths = Array.Empty<string>();
            
            [NonSerialized] private Dictionary<string,string> cachedEventGuidToPreviousSyntaxPathDictionary
                = new Dictionary<string, string>();
            [NonSerialized] private bool didCacheEventGuidToPreviousSyntaxPathDictionary;
            public Dictionary<string,string> EventGuidToPreviousSyntaxPath
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
                    List<string> previousEventSyntaxPathValues = new List<string>(); 
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
                string version, FmodSyntaxSettings.EventNameClashPreventionTypes clashPreventionType,
                Dictionary<string, string> eventGuidToPreviousSyntaxPaths)
            {
                this.version = version;
                this.clashPreventionType = clashPreventionType;
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
            None,             // Could not be parsed.
            ActiveEventGuids, // The old format that just specified which events are currently being used and their name
            Json,             // The new format that still stores old event data but also supports storing additional data
        }
        
        private static string ScriptPathBase => FmodSyntaxSettings.Instance.GeneratedScriptsFolderPath;
        private const string TemplatePathBase = "Templates/Fmod/";
        
        private static string EventsScriptPath => ScriptPathBase + "FmodEvents.cs";
        private static string EventsScriptTypesPath => ScriptPathBase + "FmodEventsTypes.cs";
        private const string EventsTemplatePath = TemplatePathBase + "Events/";
        
        private static string BanksScriptPath => ScriptPathBase + "FmodBanks.cs";
        private const string BanksTemplatePath = TemplatePathBase + "Banks/";
        
        private static string BusesScriptPath => ScriptPathBase + "FmodBuses.cs";
        private const string BusesTemplatePath = TemplatePathBase + "Buses/";
        
        private static string SnapshotsScriptPath => ScriptPathBase + "FmodSnapshots.cs";
        private const string SnapshotsTemplatePath = TemplatePathBase + "Snapshots/";
        
        private static string VCAsScriptPath => ScriptPathBase + "FmodVCAs.cs";
        private const string VCAsTemplatePath = TemplatePathBase + "VCAs/";

        private static readonly CodeGenerator assemblyDefinitionGenerator =
            new CodeGenerator(TemplatePathBase + "FMOD-Syntax.asmdef");

        private const string EventNameKeyword = "EventName";

        private static readonly CodeGenerator eventsScriptGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEvents.cs");
        private static readonly CodeGenerator eventTypesScriptGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventTypes.cs");
        private static readonly CodeGenerator eventTypeGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventType.cs");
        private static readonly CodeGenerator eventFieldGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventField.cs");
        private static readonly CodeGenerator eventParameterGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventParameter.cs");
        private static readonly CodeGenerator eventParametersInitializationGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventParametersInitialization.cs");
        private static readonly CodeGenerator eventConfigPlayMethodWithParametersGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventConfigPlayMethodWithParameters.cs");
        private static readonly CodeGenerator eventPlaybackPlayMethodWithParametersGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventPlaybackPlayMethodWithParameters.cs");
        
        private static readonly CodeGenerator eventFolderGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventFolder.cs");

        private static readonly CodeGenerator enumGenerator = new CodeGenerator(EventsTemplatePath + "FmodEnum.cs");
        
        private static string GlobalParametersScriptPath => ScriptPathBase + "FmodGlobalParameters.cs";
        private static readonly CodeGenerator globalParametersGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodGlobalParameters.cs");
        private static readonly CodeGenerator globalParameterGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodGlobalParameter.cs");
        
        private static readonly CodeGenerator banksScriptGenerator =
            new CodeGenerator(BanksTemplatePath + "FmodBanks.cs");
        private static readonly CodeGenerator bankFieldGenerator =
            new CodeGenerator(BanksTemplatePath + "FmodBankField.cs");
        
        private static readonly CodeGenerator busesScriptGenerator =
            new CodeGenerator(BusesTemplatePath + "FmodBuses.cs");
        private static readonly CodeGenerator busFieldGenerator =
            new CodeGenerator(BusesTemplatePath + "FmodBusField.cs");
        
        private static readonly CodeGenerator snapshotsScriptGenerator =
            new CodeGenerator(SnapshotsTemplatePath + "FmodSnapshots.cs");
        private static readonly CodeGenerator snapshotFieldsGenerator =
            new CodeGenerator(SnapshotsTemplatePath + "FmodSnapshotFields.cs");
        
        private static readonly CodeGenerator vcasScriptGenerator =
            new CodeGenerator(VCAsTemplatePath + "FmodVCAs.cs");
        private static readonly CodeGenerator vcaFieldGenerator =
            new CodeGenerator(VCAsTemplatePath + "FmodVCAField.cs");

        private static EventFolder rootEventFolder;
        
        private static FmodSyntaxSettings Settings => FmodSyntaxSettings.Instance;
        
        [NonSerialized] private static List<string> eventUsingDirectives = new List<string>();
        [NonSerialized] private static string eventUsingDirectivesCode;
        [NonSerialized] private static string[] eventUsingDirectivesDefault =
        {
            "System",
            "System.Collections.Generic",
            "FMOD.Studio",
            "RoyTheunissen.FMODSyntax",
            "UnityEngine",
            "UnityEngine.Scripting",
        };

        [NonSerialized] private static bool didSourceFilesChange;
        
        [NonSerialized] private static string parameterlessEventsCode = "";
        
        [NonSerialized] private static string rawMetaDataFromPreviousCodeGeneration;
        [NonSerialized] private static MetaDataFormats metaDataFormatFromPreviousCodeGeneration;
        [NonSerialized] private static MetaData metaDataFromPreviousCodeGeneration;

        [NonSerialized]
        private static Dictionary<string, string> activeEventGuidToCurrentSyntaxPath = new Dictionary<string, string>();
        
        [NonSerialized] private static readonly Dictionary<string, Type> labelParameterNameToUserSpecifiedEnumType 
            = new Dictionary<string, Type>();
        [NonSerialized] private static bool didCacheUserSpecifiedEnums;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            labelParameterNameToUserSpecifiedEnumType.Clear();
            didCacheUserSpecifiedEnums = false;

            // NOTE: For this to work, SourceFilesChangedEvent and BankRefreshEvent events need to be added to FMOD.
#if FMOD_AUTO_REGENERATE_CODE
            // Need to both keep track of when the source files have changed and when a refresh occurs, because
            // bank refreshes happen a lot even when nothing's changed, but we can't directly respond to source file
            // changes because it's too soon. Banks are only refreshed after a delay.
            BankRefresher.SourceFilesChangedEvent -= HandleSourceFilesChangedEvent;
            BankRefresher.SourceFilesChangedEvent += HandleSourceFilesChangedEvent;
            
            BankRefresher.BankRefreshEvent -= HandleBankRefreshEvent;
            BankRefresher.BankRefreshEvent += HandleBankRefreshEvent;
#endif // FMOD_AUTO_REGENERATE_CODE
        }

#if FMOD_AUTO_REGENERATE_CODE
        private static void HandleSourceFilesChangedEvent()
        {
            didSourceFilesChange = true;
        }

        private static void HandleBankRefreshEvent()
        {
            if (!didSourceFilesChange)
                return;

            didSourceFilesChange = false;

            // When the banks are refreshed and the source files have ACTUALLY changed, automatically re-generate code
            // if the preferences allow us to. Otherwise it should be done manually via FMOD>Generate Code
            if (!FmodPreferences.GenerateCodeAutomaticallyPreference.Value)
                return;

            GenerateCode();
        }
#endif // FMOD_AUTO_REGENERATE_CODE

        [MenuItem("FMOD/Cache Enums")]
        private static void CacheUserSpecifiedLabelParameterEnums()
        {
            if (didCacheUserSpecifiedEnums)
                return;

            didCacheUserSpecifiedEnums = true;
            
            labelParameterNameToUserSpecifiedEnumType.Clear();
            Type[] enums = TypeExtensions.GetAllTypesWithAttribute<FmodLabelEnumAttribute>();
            for (int i = 0; i < enums.Length; i++)
            {
                Type enumType = enums[i];
                FmodLabelEnumAttribute attribute = enumType.GetAttribute<FmodLabelEnumAttribute>();

                for (int j = 0; j < attribute.LabelledParameterNames.Length; j++)
                {
                    string parameterName = attribute.LabelledParameterNames[j];
                    bool succeeded = labelParameterNameToUserSpecifiedEnumType.TryAdd(parameterName, enumType);
                    if (!succeeded)
                    {
                        Type existingEnumType = labelParameterNameToUserSpecifiedEnumType[parameterName];
                        Debug.LogError($"Enum '{enumType.Name}' tried to map labelled parameters with name " +
                                         $"'{parameterName}' via [FmodLabelEnum], but that was already mapped to " +
                                         $"enum '{existingEnumType.Name}'. Make sure there is only one such mapping.");
                    }
                }
            }
        }

        public static bool GetUserSpecifiedLabelParameterEnum(string name, out Type enumType)
        {
            CacheUserSpecifiedLabelParameterEnums();
            
            return labelParameterNameToUserSpecifiedEnumType.TryGetValue(name, out enumType);
        }

        private static string GetParameterCode(CodeGenerator codeGenerator, EditorParamRef parameter)
        {
            // Generate a public static readonly field for this parameter with the specified GUID. Note that this is
            // done both for local event parameters as well as global event parameters.
            codeGenerator.Reset();
            string name = parameter.GetFilteredName();
            string type = parameter.GetWrapperType();
            codeGenerator.ReplaceKeyword("ParameterType", type);
            codeGenerator.ReplaceKeyword("ParameterName", name);
            codeGenerator.ReplaceKeyword("ID1", parameter.ID.data1.ToString());
            codeGenerator.ReplaceKeyword("ID2", parameter.ID.data2.ToString());

            // "Labelled parameters" require us to also generate an enum for its possible values.
            const string enumKeyword = "ParameterEnum";
            if (parameter.Type == ParameterType.Labeled)
            {
                bool hasUserSpecifiedEnum = GetUserSpecifiedLabelParameterEnum(parameter.Name, out Type enumType);
                if (hasUserSpecifiedEnum)
                {
                    // Not generating a new enum.
                    codeGenerator.RemoveKeywordLines(enumKeyword);
                }
                else
                {
                    string enumValues = string.Empty;
                    for (int i = 0; i < parameter.Labels.Length; i++)
                    {
                        enumValues += $"{FmodSyntaxUtilities.GetFilteredNameFromPath(parameter.Labels[i])}";
                        if (i < parameter.Labels.Length - 1)
                            enumValues += ",";
                        enumValues += "\r\n";
                    }

                    enumGenerator.Reset();
                    enumGenerator.ReplaceKeyword("Name", $"{name}Values");
                    enumGenerator.ReplaceKeyword("Values", enumValues);
                    string enumCode = enumGenerator.GetCode();
                    
                    codeGenerator.ReplaceKeyword(enumKeyword, enumCode);
                }
            }
            else
            {
                // If there's no enum then we can just get rid of that keyword.
                codeGenerator.RemoveKeywordLines(enumKeyword);
            }
                
            return codeGenerator.GetCode();
        }

        private static string GetCurrentVersionNumber()
        {
            // Get the data from the package.json file.
            string packageJsonAssetPath = AssetDatabase.GUIDToAssetPath("9381d3b6ee2b4dc47a6dccb6aacaed4a");
            TextAsset packageJsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(packageJsonAssetPath);
            string packageJson = packageJsonFile.text;
            
            // Try to find the part where it starts defining the version.
            const string versionStartSyntax = "\"version\": \"";
            int versionStartIndex = packageJson.IndexOf(versionStartSyntax, StringComparison.OrdinalIgnoreCase);
            if (versionStartIndex == -1)
                return string.Empty;
            versionStartIndex += versionStartSyntax.Length;

            // Try to find where the version string ends.
            int versionEndIndex = packageJson.IndexOf("\"", versionStartIndex + 1, StringComparison.Ordinal);
            if (versionEndIndex == -1)
                return string.Empty;

            // Return the version number string.
            return packageJson.Substring(versionStartIndex, versionEndIndex - versionStartIndex);
        }

        private static string GetRawMetaData(out MetaDataFormats format)
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
            const string newMetaDataStart = "/* ---------------------------------------------- METADATA ------------------------------------------------------";
            const string newMetaDataEnd = "------------------------------------------------- METADATA --------------------------------------------------- */";
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
            rawMetaDataFromPreviousCodeGeneration = GetRawMetaData(out metaDataFormatFromPreviousCodeGeneration);
            
            if (metaDataFormatFromPreviousCodeGeneration == MetaDataFormats.None)
            {
                // Metadata could not be found. Just assume the default empty metadata.
                metaDataFromPreviousCodeGeneration = new MetaData();
            }
            else if (metaDataFormatFromPreviousCodeGeneration == MetaDataFormats.ActiveEventGuids)
            {
                // The metadata was in the old format. This one did not support name clash prevention nor did it specify
                // a version number. Only event GUIDs and their old name.
                string version = "0.0.1";
                
                FmodSyntaxSettings.EventNameClashPreventionTypes clashPreventionType =
                    FmodSyntaxSettings.EventNameClashPreventionTypes.None;
                
                Dictionary<string, string> eventGuidToPreviousSyntaxPath = GetExistingEventSyntaxPathsByGuid();
                
                metaDataFromPreviousCodeGeneration = new MetaData(
                    version, clashPreventionType, eventGuidToPreviousSyntaxPath);
            }
            else
            {
                // The metadata was in the new JSON format so we can just deserialize it.
                metaDataFromPreviousCodeGeneration = JsonUtility.FromJson<MetaData>(
                    rawMetaDataFromPreviousCodeGeneration);
            }
        }

        private static Dictionary<string, string> GetExistingEventSyntaxPathsByGuid()
        {
            Dictionary<string, string> existingEventPathsByGuid = new Dictionary<string, string>();
            
            // Every line is an individual event formatted as path=guid
            string[] lines = rawMetaDataFromPreviousCodeGeneration.Split("\r\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimStart();
                string[] nameAndGuid = line.Split("=");
                if (nameAndGuid.Length != 2)
                    continue;
                        
                string path = nameAndGuid[0];
                string guid = nameAndGuid[1];
                        
                existingEventPathsByGuid.Add(guid, path);
            }

            return existingEventPathsByGuid;
        }
        
        private static string GetEventTypeCode(EditorEventRef e, string eventName = "", string attribute = "")
        {
            if (string.IsNullOrEmpty(eventName))
                eventName = GetEventSyntaxName(e);
            
            eventTypeGenerator.Reset();
            eventTypeGenerator.ReplaceKeyword(EventNameKeyword, eventName);
            
            // Parameters
            GetEventTypeParametersCode(e, eventName);

            const string attributesKeyword = "Attributes";
            if (string.IsNullOrEmpty(attribute))
                eventTypeGenerator.RemoveKeywordLines(attributesKeyword);
            else
                eventTypeGenerator.ReplaceKeyword(attributesKeyword, attribute);
            
            return eventTypeGenerator.GetCode();
        }

        private static void GetEventTypeParametersCode(EditorEventRef e, string eventName)
        {
            const string eventParametersKeyword = "EventParameters";
            const string configPlayMethodWithParametersKeyword = "ConfigPlayMethodWithParameters";
            
            // If there's no parameters for this event then we can just get rid of the keywords and leave it there.
            if (e.LocalParameters.Count <= 0)
            {
                eventTypeGenerator.RemoveKeywordLines(configPlayMethodWithParametersKeyword);
                eventTypeGenerator.RemoveKeywordLines(eventParametersKeyword);
                return;
            }

            string eventParametersCode = string.Empty;

            string parameterArguments = string.Empty;
            string parameterArgumentsWithType = string.Empty;
            string parameterArgumentsWithTypeFullyQualified = string.Empty;
            string parameterInitializationsFromArguments = string.Empty;
            for (int i = 0; i < e.LocalParameters.Count; i++)
            {
                EditorParamRef parameter = e.LocalParameters[i];
                
                // Define a new local parameter for this event.
                eventParametersCode += GetParameterCode(eventParameterGenerator, parameter);

                // Also cache this for generating custom play methods with all the parameters in it, it's to support
                // a syntax like 'Events.Footstep.Play(SurfaceValues.Dirt)' where the Play method has strongly-typed
                // arguments for all of its parameters.
                string parameterName = parameter.GetFilteredName();
                string argumentName = parameter.GetArgumentName();
                string argumentType = parameter.GetArgumentType();
                string argumentTypeFullyQualified = parameter.GetArgumentTypeFullyQualified(e);
                string spacing = i == 0 ? string.Empty : ", ";
                parameterArguments += spacing + $"{argumentName}";
                parameterArgumentsWithType += spacing + $"{argumentType} {argumentName}";
                parameterArgumentsWithTypeFullyQualified +=
                    spacing + $"{argumentTypeFullyQualified} {argumentName}";
                parameterInitializationsFromArguments +=
                    $"{parameterName}.Value = {argumentName};\r\n";
            }

            // THEN write an InitializeParameters function to pass along the instance to the parameters.
            string eventParametersInitializationCode = string.Empty;
            foreach (EditorParamRef parameter in e.LocalParameters)
            {
                string parameterName = parameter.GetFilteredName();
                eventParametersInitializationCode += $"{parameterName}.InitializeAsEventParameter(Instance);\r\n";
            }

            eventParametersInitializationGenerator.Reset();
            eventParametersInitializationGenerator.ReplaceKeyword(
                "EventParametersInitialization", eventParametersInitializationCode);
            eventParametersCode += eventParametersInitializationGenerator.GetCode();

            // Now generate a custom play method with all the parameters for the playback.
            if (e.IsOneShot)
            {
                eventPlaybackPlayMethodWithParametersGenerator.Reset();
                eventPlaybackPlayMethodWithParametersGenerator.ReplaceKeyword(
                    "ParameterArgumentsWithType", parameterArgumentsWithType);
                eventPlaybackPlayMethodWithParametersGenerator.ReplaceKeyword(
                    "ParameterArguments", parameterArguments);
                eventPlaybackPlayMethodWithParametersGenerator.ReplaceKeyword(
                    "ParameterInitializationsFromArguments", parameterInitializationsFromArguments);
                eventParametersCode += eventPlaybackPlayMethodWithParametersGenerator.GetCode();

                // Also generate a custom play method with all the parameters in it for the config, which is what the user
                // will call, which just forwards it to the playback instance.
                eventConfigPlayMethodWithParametersGenerator.Reset();
                eventConfigPlayMethodWithParametersGenerator.ReplaceKeyword("EventName", eventName);
                eventConfigPlayMethodWithParametersGenerator.ReplaceKeyword(
                    "ParameterArgumentsWithType", parameterArgumentsWithTypeFullyQualified);
                eventConfigPlayMethodWithParametersGenerator.ReplaceKeyword(
                    "ParameterArguments", parameterArguments);
                
                eventTypeGenerator.ReplaceKeyword(
                    configPlayMethodWithParametersKeyword,
                    eventConfigPlayMethodWithParametersGenerator.GetCode());
            }
            else
            {
                eventTypeGenerator.RemoveKeywordLines(configPlayMethodWithParametersKeyword);
            }
            
            eventTypeGenerator.ReplaceKeyword(eventParametersKeyword, eventParametersCode);
        }
        
        /// <summary>
        /// Gets the name of an event *as it is represented in the AudioEvents syntax*. For example, here's an event
        /// called Core/Player/Footstep for different Name Clash Prevention types:
        /// None:                                   Footstep
        /// Generate Separate Classes Per Folder:   Footstep
        /// Include Path:                           Core_Player_Footstep
        /// </summary>
        private static string GetEventSyntaxName(string filteredPath)
        {
            // If specified, include the entire path as a prefix.
            if (Settings.EventNameClashPreventionType == FmodSyntaxSettings.EventNameClashPreventionTypes.IncludePath)
                return filteredPath.Replace("_", "").Replace("/", "_");
            
            return FmodSyntaxUtilities.GetFilteredNameFromPath(filteredPath);
        }
        
        private static string GetEventSyntaxName(EditorEventRef e)
        {
            return GetEventSyntaxName(e.GetFilteredPath(true));
        }
        
        /// <summary>
        /// Gets the path of an event *as it is represented in the AudioEvents syntax*. For example, here's an event
        /// called Core/Player/Footstep for different Name Clash Prevention types:
        /// None:                                   Footstep
        /// Generate Separate Classes Per Folder:   Core/Player/Footstep
        /// Include Path:                           Core_Player_Footstep
        /// </summary>
        private static string GetEventSyntaxPath(string filteredPath)
        {
            string eventName = GetEventSyntaxName(filteredPath);
            
            if (Settings.EventNameClashPreventionType !=
                FmodSyntaxSettings.EventNameClashPreventionTypes.GenerateSeparateClassesPerFolder)
            {
                return eventName;
            }
            
            string eventDirectories = Path.GetDirectoryName(filteredPath);
            
            return Path.Combine(eventDirectories, eventName).ToUnityPath();
        }

        private static string GetEventSyntaxPath(EditorEventRef e)
        {
            return GetEventSyntaxPath(e.GetFilteredPath(true));
        }

        private static string GetEventCode(EditorEventRef e, string eventName = "", string attribute = "")
        {
            // By default we use the event's own name, but when we're generating aliases we actually want to generate
            // a Config/Playback that has an old name but points to the GUID of the newly named event, so we want to 
            // be able to specify a different name in that use case.
            if (string.IsNullOrEmpty(eventName))
                eventName = GetEventSyntaxName(e);
            string fieldName = FmodSyntaxUtilities.GetFilteredNameFromPathLowerCase(eventName);
            
            eventFieldGenerator.Reset();
            eventFieldGenerator.ReplaceKeyword(EventNameKeyword, eventName);
            eventFieldGenerator.ReplaceKeyword("eventName", fieldName);
            eventFieldGenerator.ReplaceKeyword("GUID", e.Guid.ToString());
            
            // Aliases have an Obsolete attribute, normal events don't and can just remove the keyword.
            if (string.IsNullOrEmpty(attribute))
                eventFieldGenerator.RemoveKeywordLines("Attributes");
            else
                eventFieldGenerator.ReplaceKeyword("Attributes", attribute);
            
            return eventFieldGenerator.GetCode();
        }

        /// <summary>
        /// JsonUtility does not support serializing dictionaries, so we can instead serialize it as an array of strings.
        /// Unfortunately then every key and value ends up on a separate line. For readability it would be great if
        /// those were together on one line. So this does a small pass where it takes a dictionary and puts the key
        /// and value lines on the same line but preserves indentation and whatnot.
        /// </summary>
        private static string CleanUpJsonDictionarySyntax(string json, string dictionaryName)
        {
            // Find out where the dictionary starts.
            string dictionaryStartKeyword = $"\"{dictionaryName}\": [\n";
            int startIndex = json.IndexOf(dictionaryStartKeyword, StringComparison.Ordinal);
            if (startIndex == -1)
                return json;
            startIndex += dictionaryStartKeyword.Length;
            
            // Find out where the dictionary ends.
            int endIndex = json.IndexOf("]", startIndex + 1, StringComparison.Ordinal);
            if (endIndex == -1)
                return json;

            // Figure out the sections of the dictionary itself as well as before and after it.
            string preDictionarySection = json.Substring(0, startIndex);
            string dictionarySection = json.Substring(startIndex, endIndex - startIndex);
            string postDictionarySection = json.Substring(endIndex);
            
            // Now let's clean up the dictionary section.
            string[] dictionarySectionLines = dictionarySection.Split("\n");
            List<string> combinedDictionarySectionLines = new List<string>();
            
            // NOTE: The last line is empty so skip that one.
            for (int i = 0; i < dictionarySectionLines.Length; i += 2)
            {
                // The last line is empty but it has indentation so keep that as-is.
                if (i == dictionarySectionLines.Length - 1)
                {
                    combinedDictionarySectionLines.Add(dictionarySectionLines[i]);
                    break;
                }
                
                // Find out what the key and value are.
                string key = dictionarySectionLines[i];
                string value = dictionarySectionLines[i + 1];
                
                // Now combine them in such a way that there is no linebreak between them, just a space.
                // Preserve existing indentation.
                key = key.TrimEnd();
                value = value.TrimStart();
                string combinedLine = key + " " + value;
                combinedDictionarySectionLines.Add(combinedLine);
            }
            dictionarySection = string.Join("\n", combinedDictionarySectionLines);
            
            return preDictionarySection + dictionarySection + postDictionarySection;
        }

        [MenuItem("FMOD/Generate FMOD Code %&g", false, 999999999)]
        public static void GenerateCode()
        {
            if (Settings.ShouldGenerateAssemblyDefinition)
                GenerateAssemblyDefinition();
            
            ParseMetaData();
            
            GenerateEventsScript(true, EventsScriptPath);
            GenerateEventsScript(false, EventsScriptTypesPath);
            
            // NOTE: This re-uses the using directives from the generated events. Therefore this should be called after
            // the events are generated.
            GenerateGlobalParametersScript();
            
            GenerateMiscellaneousScripts();
        }

        private static void GenerateAssemblyDefinition()
        {
            assemblyDefinitionGenerator.Reset();
            
            assemblyDefinitionGenerator.ReplaceKeyword("Name", Settings.NamespaceForGeneratedCode);
            assemblyDefinitionGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);
            
            assemblyDefinitionGenerator.GenerateFile(ScriptPathBase + $"{Settings.NamespaceForGeneratedCode}.asmdef");
        }

        private static void GenerateGlobalParametersScript()
        {
            // Also generate a field for every FMOD global parameter.
            string globalParametersCode = string.Empty;
            foreach (EditorParamRef parameter in EventManager.Parameters)
            {
                globalParametersCode += GetParameterCode(globalParameterGenerator, parameter);
            }
            
            globalParametersGenerator.Reset();
            globalParametersGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);
            
            // NOTE: This re-uses the using directives from the generated events. Therefore this should be called after
            // the events are generated.
            globalParametersGenerator.ReplaceKeyword("UsingDirectives", eventUsingDirectivesCode);
            
            globalParametersGenerator.ReplaceKeyword("GlobalParameters", globalParametersCode);
            globalParametersGenerator.GenerateFile(GlobalParametersScriptPath);
        }

        private static void GenerateEventsScript(bool isDeclaration, string eventsScriptPath)
        {
            eventUsingDirectives.Clear();
            eventUsingDirectives.AddRange(eventUsingDirectivesDefault);

            // We either only declare the events or we define the events. Separating this out into separate files
            // makes it easier to just have a look at which events were generated at all.
            CodeGenerator codeGenerator = isDeclaration ? eventsScriptGenerator : eventTypesScriptGenerator;
            codeGenerator.Reset();
            
            codeGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);

            // Generate a config & playback class for every FMOD event.
            EditorEventRef[] events = EventManager.Events
                .Where(e => e.Path.StartsWith(EditorEventRefExtensions.EventPrefix))
                .OrderBy(e => e.Path).ToArray();
            
            Debug.Log($"GENERATING EVENTS SCRIPT NOW!");

            // Organize the events in a folder hierarchy.
            rootEventFolder = new EventFolder("AudioEvents");
            foreach (EditorEventRef e in events)
            {
                string path = e.GetFilteredPath(true);

                EventFolder folder = rootEventFolder;
                
                if (Settings.EventNameClashPreventionType ==
                    FmodSyntaxSettings.EventNameClashPreventionTypes.GenerateSeparateClassesPerFolder)
                {
                    folder = rootEventFolder.GetOrCreateChildFolderFromPathRecursively(path);
                }

                folder.ChildEvents.Add(e);
                
                string currentPath = path;
                
                bool eventExistedDuringPreviousCodeGeneration = metaDataFromPreviousCodeGeneration
                    .EventGuidToPreviousSyntaxPath.TryGetValue(e.Guid.ToString(), out string previousSyntaxPath);
                bool shouldGenerateAlias = false;
                if (eventExistedDuringPreviousCodeGeneration)
                {
                    string currentSyntaxPath = GetEventSyntaxPath(currentPath);
                    shouldGenerateAlias = previousSyntaxPath != currentSyntaxPath;
                }
                
                // Also generate aliases for this event if it has been renamed so you have a chance to update the
                // code without it breaking. Outputs some nice warnings instead via an Obsolete attribute.
                if (shouldGenerateAlias)
                {
                    EventFolder previousFolder = rootEventFolder
                        .GetOrCreateChildFolderFromPathRecursively(previousSyntaxPath);

                    previousFolder.ChildEventToAliasPath[e] = previousSyntaxPath;
                }
            }
            
            // Generate code for the events per folder.
            parameterlessEventsCode = "";
            string eventsCode;
            if (!isDeclaration)
            {
                // If we don't separate events with folders then we define the event types outside of the root folder,
                // which is how it used to work and prevents you from having to type
                // 'AudioEvents.NameOfEventPlayback playback;' and lets you type
                // 'NameOfEventPlayback playback;' instead (without the 'AudioEvents.')
                eventsCode = GenerateFolderCode(
                    rootEventFolder, isDeclaration, out string eventTypesCodeToBePlacedOutsideOfRootFolder);
                eventsCode = eventTypesCodeToBePlacedOutsideOfRootFolder + eventsCode;
            }
            else
            {
                eventsCode = GenerateFolderCode(rootEventFolder, isDeclaration);
            }
            codeGenerator.ReplaceKeyword("Events", eventsCode, true);
            
            if (isDeclaration)
            {
                codeGenerator.ReplaceKeyword("ParameterlessEventIds", parameterlessEventsCode, true);

                // Write new metadata to the file while with the current data while preserving the old one,
                // because we may still need that one for generating other files.
                string versionNumber = GetCurrentVersionNumber();
                FmodSyntaxSettings.EventNameClashPreventionTypes clashPreventionType =
                    Settings.EventNameClashPreventionType;
                MetaData newMetaData = new MetaData(
                    versionNumber, clashPreventionType, activeEventGuidToCurrentSyntaxPath);
                codeGenerator.ReplaceKeyword("MetaData", newMetaData.GetJson(), true);
            }

            // Also allow custom using directives to be specified.
            eventUsingDirectivesCode = string.Empty;
            for (int i = 0; i < eventUsingDirectives.Count; i++)
            {
                string usingDirective = eventUsingDirectives[i];
                eventUsingDirectivesCode += $"using {usingDirective};\r\n";
            }
            codeGenerator.ReplaceKeyword("UsingDirectives", eventUsingDirectivesCode);

            codeGenerator.GenerateFile(eventsScriptPath);
        }

        private static string GenerateFolderCode(
            EventFolder eventFolder, bool isDeclaration, out string eventTypesCodeToBePlacedOutsideOfRootFolder)
        {
            string childFoldersCode = "";
            for (int i = 0; i < eventFolder.ChildFolders.Count; i++)
            {
                EventFolder childFolder = eventFolder.ChildFolders[i];
                string childFolderCode = GenerateFolderCode(childFolder, isDeclaration);
                childFoldersCode += childFolderCode + "\r\n";
            }
            
            // Add an extra linebreak if there's subfolders.
            if (!string.IsNullOrEmpty(childFoldersCode))
                childFoldersCode += "\r\n";
            
            // NOTE: Do this AFTER we have gathered code from child folders because there is recursion there. Otherwise
            // our children would reset the event folder generator that we're using.
            eventFolderGenerator.Reset();
            
            eventFolderGenerator.ReplaceKeyword("FolderName", eventFolder.Name);
            eventFolderGenerator.ReplaceKeyword("FolderTypeName", eventFolder.Name + "Folder");
            eventFolderGenerator.ReplaceKeyword("Subfolders", childFoldersCode, true);
            
            string eventTypeAliasesCode = "";
            string eventTypeAliasesCodeThatUsedToBeOutsideRootFolder = "";
            string eventAliasesCode = "";
            string eventTypesCode = string.Empty;
            string eventsCode = string.Empty;
            foreach (EditorEventRef e in eventFolder.ChildEvents)
            {
                // Log it in the active event GUIDs. This way we can keep track of which events we had previously and
                // which paths / GUIDs they had. Then we can figure out if they were renamed/moved, then add those
                // back with an alias and an Obsolete tag so everything doesn't break immediately and you get a chance
                // to fix your code first.
                // NOTE: We don't *have* to keep track of them this way necessarily, we could intercept UpdateCache
                // in EventManager.cs and make it expose a list of renamed events. Would require changing FMOD even
                // further though, and the changes are already stacking up...
                if (isDeclaration)
                    activeEventGuidToCurrentSyntaxPath[e.Guid.ToString()] = GetEventSyntaxPath(e);

                // Types
                if (!isDeclaration)
                    eventTypesCode += GetEventTypeCode(e);

                // Fields
                if (isDeclaration)
                    eventsCode += GetEventCode(e);

                if (e.LocalParameters.Count == 0)
                {
                    parameterlessEventsCode += $"{{ \"{e.Guid}\", new FmodParameterlessAudioConfig(\"{e.Guid}\") }},\r\n";
                }
            }

            // Generate code for event aliases.
            if (Settings.GenerateFallbacksForMissingEvents)
            {
                foreach (KeyValuePair<EditorEventRef, string> eventPreviousPathPair in
                         eventFolder.ChildEventToAliasPath)
                {
                    EditorEventRef e = eventPreviousPathPair.Key;
                    string currentSyntaxPath = GetEventSyntaxPath(e);
                    string previousSyntaxPath = eventPreviousPathPair.Value;

                    string previousName = Path.GetFileName(previousSyntaxPath);

                    string attribute =
                        $"[Obsolete(\"FMOD Event '{previousSyntaxPath}' has been changed to '{currentSyntaxPath}'\")]";

                    if (isDeclaration)
                        eventAliasesCode += GetEventCode(e, previousName, attribute);
                    else
                    {
                        string eventTypeAliasCode = GetEventTypeCode(e, previousName, attribute);
                        
                        if (metaDataFromPreviousCodeGeneration.ClashPreventionType != FmodSyntaxSettings
                                .EventNameClashPreventionTypes.GenerateSeparateClassesPerFolder)
                        {
                            eventTypeAliasesCodeThatUsedToBeOutsideRootFolder += eventTypeAliasCode;
                        }
                        else
                        {
                            eventTypeAliasesCode += eventTypeAliasCode;
                        }
                    }
                }
            }

            // Also add a section for any event type aliases, if needed.
            const string eventTypeAliasesKeyword = "EventTypeAliases";
            if (string.IsNullOrEmpty(eventTypeAliasesCode))
            {
                eventTypeAliasesCode = string.Empty;
            }
            else
            {
                // NOTE: We disable obsolete warnings for the classes themselves, as technically the Config class
                // is using the obsolete Playback class, but we only want obsolete warnings where it is ACTUALLY
                // used in the codebase by developers.
                eventTypeAliasesCode = "\r\n// Aliases for event types that have been renamed:\r\n"
                                       + "#pragma warning disable 618\r\n"
                                       + eventTypeAliasesCode
                                       + "#pragma warning restore 618\r\n";
            }
            
            // If we separate events with folders then we define the types inside the folder in question. Otherwise we
            // only have one root folder, and we define the types outside of that, which is how it used to work and
            // prevents you from having to type 'AudioEvents.NameOfEventPlayback playback;' and lets you type
            // 'NameOfEventPlayback playback;' instead, without the 'AudioEvents.'
            if (Settings.EventNameClashPreventionType 
                == FmodSyntaxSettings.EventNameClashPreventionTypes.GenerateSeparateClassesPerFolder)
            {
                eventFolderGenerator.ReplaceKeyword("EventTypes", eventTypesCode, true);
                eventFolderGenerator.ReplaceKeyword(eventTypeAliasesKeyword, eventTypeAliasesCode);
                
                // Most event type aliases code should go inside the appropriate folder, but if code was previously
                // generated with a folderless name clash prevention then those type aliases should be placed OUTSIDE
                // of the root folder because that's where the types in question used to be declared.
                eventTypesCodeToBePlacedOutsideOfRootFolder = eventTypeAliasesCodeThatUsedToBeOutsideRootFolder;
            }
            else
            {
                if (metaDataFromPreviousCodeGeneration.ClashPreventionType == FmodSyntaxSettings
                        .EventNameClashPreventionTypes.GenerateSeparateClassesPerFolder)
                {
                    // Previously we did use folders, so place the type aliases in the appropriate folder instead of
                    // next to the root folder like we would for our current clash prevention type.
                    eventFolderGenerator.ReplaceKeyword(eventTypeAliasesKeyword, eventTypeAliasesCode);
                }
                else
                {
                    eventFolderGenerator.RemoveKeywordLines(eventTypeAliasesKeyword);

                    // When we don't use folders we actually define event types *next* to AudioEvents (the root folder)
                    // and not inside the root folder. That way you don't have to specify event types like
                    // 'AudioEvents.FootstepPlayback' but just like 'FootstepPlayback' which is simpler.
                    if (!string.IsNullOrEmpty(eventTypeAliasesCode))
                        eventTypesCode += "\r\n" + eventTypeAliasesCode + "\r\n";
                }
                
                eventFolderGenerator.RemoveKeywordLines("EventTypes");
                eventTypesCodeToBePlacedOutsideOfRootFolder = eventTypesCode;
            }
            
            eventFolderGenerator.ReplaceKeyword("Events", eventsCode, true);
            
            // Also add a section for any event aliases, if needed.
            const string eventAliasesKeyword = "EventAliases";
            if (string.IsNullOrEmpty(eventAliasesCode) || !Settings.GenerateFallbacksForMissingEvents)
            {
                eventFolderGenerator.RemoveKeywordLines(eventAliasesKeyword);
            }
            else
            {
                eventAliasesCode = "\r\n// Aliases for events that have been changed:\r\n" + eventAliasesCode;
                eventFolderGenerator.ReplaceKeyword(eventAliasesKeyword, eventAliasesCode);
            }

            string baseType = isDeclaration ? "" : " : FmodAudioFolder";
            eventFolderGenerator.ReplaceKeyword("BaseType", baseType);
            
            return eventFolderGenerator.GetCode();
        }

        private static string GenerateFolderCode(EventFolder eventFolder, bool isDeclaration)
        {
            return GenerateFolderCode(eventFolder, isDeclaration, out string _);
        }

        private static void GenerateMiscellaneousScripts()
        {
            // NOTE: These are all together because that way data can be cached more easily.
            
            banksScriptGenerator.Reset();
            
            banksScriptGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);
            
            string banksCode = string.Empty;

            // Need to access the banks this way, not via EventManager.Banks because EditorBankRef doesn't have info
            // on the buses, and we need that for the sake of generating bus code.
            EditorUtils.LoadPreviewBanks();
            EditorUtils.System.getBankList(out Bank[] banks);
            
            banks = banks.OrderBy(b => b.getPath()).ToArray();
            List<FMOD.Studio.Bus> buses = new List<FMOD.Studio.Bus>();
            List<FMOD.Studio.VCA> VCAs = new List<FMOD.Studio.VCA>();
            foreach (Bank bank in banks)
            {
                bankFieldGenerator.Reset();
                string bankPath = bank.getPath();
                string bankName = bank.GetName();

                if (bankName.Contains("."))
                    continue;
                bankFieldGenerator.ReplaceKeyword("BankName", bankName);
                bankFieldGenerator.ReplaceKeyword("BankPath", bankPath);
                banksCode += bankFieldGenerator.GetCode();

                // Also figure out which buses there are. Apparently we access those via the banks.
                bank.getBusList(out FMOD.Studio.Bus[] bankBuses);
                foreach (FMOD.Studio.Bus bankBus in bankBuses)
                {
                    if (!buses.Contains(bankBus))
                        buses.Add(bankBus);
                }

                bank.getVCAList(out FMOD.Studio.VCA[] bankVCAs);
                foreach (FMOD.Studio.VCA bankVCA in bankVCAs)
                {
                    if (!VCAs.Contains(bankVCA))
                        VCAs.Add(bankVCA);
                }
            }
            
            banksScriptGenerator.ReplaceKeyword("Banks", banksCode);
            banksScriptGenerator.GenerateFile(BanksScriptPath);
            
            // Now that we know the buses, we can also generate a file for accessing those.
            busesScriptGenerator.Reset();
            
            busesScriptGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);
            
            string busesCode = string.Empty;
            buses = buses.OrderBy(b => b.getPath()).ToList();
            foreach (FMOD.Studio.Bus bus in buses)
            {
                string busPath = bus.getPath();
                string busName = bus.GetName();
                    
                if (string.IsNullOrWhiteSpace(busName))
                    busName = "Master";
                    
                busFieldGenerator.Reset();
                busFieldGenerator.ReplaceKeyword("BusName", busName);
                busFieldGenerator.ReplaceKeyword("BusPath", busPath);
                busesCode += busFieldGenerator.GetCode();
            }
            
            busesScriptGenerator.ReplaceKeyword("Buses", busesCode);
            busesScriptGenerator.GenerateFile(BusesScriptPath);
            
            // Generate a file for accessing the snapshots.
            snapshotsScriptGenerator.Reset();
            
            snapshotsScriptGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);
            
            string snapshotsCode = string.Empty;
            EditorEventRef[] snapshots = EventManager.Events
                .Where(e => e.Path.StartsWith(EditorEventRefExtensions.SnapshotPrefix))
                .OrderBy(e => e.Path).ToArray();
            foreach (EditorEventRef snapshot in snapshots)
            {
                string snapshotName = snapshot.GetFilteredName();
                string snapshotPath = snapshot.Path;
                
                snapshotFieldsGenerator.Reset();
                snapshotFieldsGenerator.ReplaceKeyword("SnapshotName", snapshotName);
                snapshotFieldsGenerator.ReplaceKeyword("GUID", snapshot.Guid.ToString());
                
                snapshotsCode += snapshotFieldsGenerator.GetCode();
            }
            
            // Also allow custom using directives to be specified.
            string usingDirectives = string.Empty;
            for (int i = 0; i < eventUsingDirectives.Count; i++)
            {
                string usingDirective = eventUsingDirectives[i];
                usingDirectives += $"using {usingDirective};\r\n";
            }
            snapshotsScriptGenerator.ReplaceKeyword("UsingDirectives", usingDirectives);
            
            snapshotsScriptGenerator.ReplaceKeyword("SnapshotTypes", snapshotsCode);
            snapshotsScriptGenerator.GenerateFile(SnapshotsScriptPath);
            
            // Now that we know the VCAs, we can also generate a file for accessing those.
            vcasScriptGenerator.Reset();
            
            vcasScriptGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);
            
            string VCAsCode = string.Empty;
            VCAs = VCAs.OrderBy(b => b.getPath()).ToList();
            foreach (FMOD.Studio.VCA VCA in VCAs)
            {
                string vcaPath = VCA.getPath();
                string vcaName = VCA.GetName();
                    
                if (string.IsNullOrWhiteSpace(vcaName))
                    continue;

                vcaFieldGenerator.Reset();
                vcaFieldGenerator.ReplaceKeyword("VCAName", vcaName);
                vcaFieldGenerator.ReplaceKeyword("VCAPath", vcaPath);
                VCAsCode += vcaFieldGenerator.GetCode();
            }
            
            vcasScriptGenerator.ReplaceKeyword("VCAs", VCAsCode);
            vcasScriptGenerator.GenerateFile(VCAsScriptPath);
        }
    }
}
