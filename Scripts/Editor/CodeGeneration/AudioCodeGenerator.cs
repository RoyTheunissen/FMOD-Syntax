using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Linq;
using UnityEngine;

#if FMOD_AUDIO_SYNTAX
using FMODUnity;
#endif // FMOD_AUDIO_SYNTAX

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Generates code for audio events & parameters.
    /// </summary>
    public static partial class AudioCodeGenerator
    {
        private static string ScriptPathBase => AudioSyntaxSettings.Instance.GeneratedScriptsFolderPath;
        private const string TemplatePathBase = "Templates/AudioSyntax/";
        
        private static string[] DeprecatedGeneratedScriptPaths => new[]
        {
            ScriptPathBase + "FmodEvents.cs",
            ScriptPathBase + "FmodEventTypes.cs",
            ScriptPathBase + "FmodBanks.cs",
            ScriptPathBase + "FmodBuses.cs",
            ScriptPathBase + "FmodSnapshots.cs",
            ScriptPathBase + "FmodSnapshotTypes.cs",
            ScriptPathBase + "FmodVCAs.cs",
            ScriptPathBase + "FmodGlobalParameters.cs",
        };
        
        private static string EventsScriptPath => ScriptPathBase + "AudioEvents.g.cs";
        private static string EventTypesScriptPath => ScriptPathBase + "AudioEventTypes.g.cs";
        private const string EventsTemplatePath = TemplatePathBase + "Events/";

        private const string EventNameKeyword = "EventName";

        private static readonly CodeGenerator eventsScriptGenerator = 
            new(EventsTemplatePath + "AudioEvents.g.cs");
        private static readonly CodeGenerator eventTypesScriptGenerator =
            new(EventsTemplatePath + "FmodEventTypes.g.cs"); // Currently FMOD-specific
        private static readonly CodeGenerator eventTypeGenerator = 
            new(EventsTemplatePath + "FmodEventType.g.cs"); // Currently FMOD-specific
        private static readonly CodeGenerator eventFieldGenerator = 
            new(EventsTemplatePath + "AudioEventField.g.cs");
        private static readonly CodeGenerator eventParameterGenerator =
            new(EventsTemplatePath + "AudioEventParameter.g.cs");
        private static readonly CodeGenerator eventParametersInitializationGenerator =
            new(EventsTemplatePath + "AudioEventParametersInitialization.g.cs");
        private static readonly CodeGenerator eventConfigPlayMethodWithParametersGenerator =
            new(EventsTemplatePath + "AudioEventConfigPlayMethodWithParameters.g.cs");
        private static readonly CodeGenerator eventPlaybackPlayMethodWithParametersGenerator =
            new(EventsTemplatePath + "AudioEventPlaybackPlayMethodWithParameters.g.cs");
        
        private static readonly CodeGenerator eventFolderGenerator = 
            new(EventsTemplatePath + "AudioEventFolder.g.cs");

        private static readonly CodeGenerator enumGenerator = 
            new(EventsTemplatePath + "AudioEnum.g.cs");
        
        private static string GlobalParametersScriptPath => ScriptPathBase + "AudioGlobalParameters.g.cs";
        private static readonly CodeGenerator globalParametersGenerator =
            new(EventsTemplatePath + "AudioGlobalParameters.g.cs");
        private static readonly CodeGenerator globalParameterGenerator =
            new(EventsTemplatePath + "FmodGlobalParameter.g.cs"); // Currently FMOD-specific
        
        private static readonly CodeGenerator banksScriptGenerator = 
            new(BanksTemplatePath + "AudioBanks.g.cs");

        private const string RefactorOldEventReferencesMenuPath = "FMOD/Refactor Old Event References";

        private static EventFolder rootEventFolder;
        
        private static AudioSyntaxSettings Settings => AudioSyntaxSettings.Instance;
        
        [NonSerialized] private static readonly List<string> eventUsingDirectives = new();
        [NonSerialized] private static string eventUsingDirectivesCode;
        [NonSerialized] private static readonly string[] eventUsingDirectivesDefault =
        {
            "System",
            "System.Collections.Generic",
            "FMOD.Studio",
            "RoyTheunissen.AudioSyntax",
            "UnityEngine",
            "UnityEngine.Scripting",
        };

        [NonSerialized] private static bool didSourceFilesChange;
        
        [NonSerialized] private static string parameterlessEventsCode = "";

        [NonSerialized]
        private static readonly Dictionary<string, string> activeEventGuidToCurrentSyntaxPath = new();
        
        private static readonly Dictionary<string, string> detectedEventChanges = new();
        
        [NonSerialized] private static readonly Dictionary<string, Type> labelParameterNameToUserSpecifiedType = new();
        [NonSerialized] private static bool didCacheUserSpecifiedEnums;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            labelParameterNameToUserSpecifiedType.Clear();
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
        
        private static void CacheUserSpecifiedLabelParameterTypes()
        {
            if (didCacheUserSpecifiedEnums)
                return;

            didCacheUserSpecifiedEnums = true;
            
            labelParameterNameToUserSpecifiedType.Clear();
            Type[] userSpecifiedTypes = TypeExtensions.GetAllTypesWithAttribute<FmodLabelTypeAttribute>();
            for (int i = 0; i < userSpecifiedTypes.Length; i++)
            {
                Type userSpecifiedType = userSpecifiedTypes[i];
                FmodLabelTypeAttribute attribute = userSpecifiedType.GetAttribute<FmodLabelTypeAttribute>();

                for (int j = 0; j < attribute.LabelledParameterNames.Length; j++)
                {
                    string parameterName = attribute.LabelledParameterNames[j];
                    bool succeeded = labelParameterNameToUserSpecifiedType.TryAdd(parameterName, userSpecifiedType);
                    if (!succeeded)
                    {
                        Type existingEnumType = labelParameterNameToUserSpecifiedType[parameterName];
                        Debug.LogError($"Enum '{userSpecifiedType.Name}' tried to map labelled parameters with name " +
                                         $"'{parameterName}' via [FmodLabelType], but that was already mapped to " +
                                         $"type '{existingEnumType.Name}'. Make sure there is only one such mapping.");
                    }
                }
            }
        }

        public static bool GetUserSpecifiedLabelParameterType(string name, out Type enumType)
        {
            CacheUserSpecifiedLabelParameterTypes();
            
            return labelParameterNameToUserSpecifiedType.TryGetValue(name, out enumType);
        }

        private static string GetParameterCode(CodeGenerator codeGenerator, AudioEventParameterDefinition parameter)
        {
            // Generate a public static readonly field for this parameter with the specified GUID. Note that this is
            // done both for local event parameters as well as global event parameters.
            codeGenerator.Reset();
            string name = parameter.FilteredName;
            string type = parameter.WrapperType;
            codeGenerator.ReplaceKeyword("ParameterType", type);
            codeGenerator.ReplaceKeyword("ParameterName", name);
            codeGenerator.ReplaceKeyword("ID1", parameter.ID1.ToString());
            codeGenerator.ReplaceKeyword("ID2", parameter.ID2.ToString());

            // "Labelled parameters" require us to also generate an enum for its possible values.
            const string enumKeyword = "ParameterEnum";
            if (parameter.IsLabeled)
            {
                bool hasUserSpecifiedEnum = GetUserSpecifiedLabelParameterType(parameter.Name, out Type enumType);
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

        private static Dictionary<string, string> GetExistingEventSyntaxPathsByGuid()
        {
            Dictionary<string, string> existingEventPathsByGuid = new();
            
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
        
        private static string GetEventTypeCode(CodeGenerator generator, AudioEventDefinition e, string eventName = "", string attribute = "")
        {
            if (string.IsNullOrEmpty(eventName))
                eventName = GetEventSyntaxName(e);
            
            generator.Reset();
            generator.ReplaceKeyword(EventNameKeyword, eventName);
            
            // Parameters
            GetEventTypeParametersCode(generator, e, eventName);

            const string attributesKeyword = "Attributes";
            if (string.IsNullOrEmpty(attribute))
                generator.RemoveKeywordLines(attributesKeyword);
            else
                generator.ReplaceKeyword(attributesKeyword, attribute);
            
            return generator.GetCode();
        }

        private static void GetEventTypeParametersCode(CodeGenerator generator, AudioEventDefinition e, string eventName)
        {
            const string eventParametersKeyword = "EventParameters";
            const string configPlayMethodWithParametersKeyword = "ConfigPlayMethodWithParameters";
            
            // If there's no parameters for this event then we can just get rid of the keywords and leave it there.
            if (e.IsParameterless)
            {
                generator.RemoveKeywordLines(configPlayMethodWithParametersKeyword);
                generator.RemoveKeywordLines(eventParametersKeyword);
                return;
            }

            string eventParametersCode = string.Empty;

            string parameterArguments = string.Empty;
            string parameterArgumentsWithType = string.Empty;
            string parameterArgumentsWithTypeFullyQualified = string.Empty;
            string parameterInitializationsFromArguments = string.Empty;
            int validParameterCount = 0;
            for (int i = 0; i < e.Parameters.Count; i++)
            {
                AudioEventParameterDefinition parameter = e.Parameters[i];

                // For snapshots we support an "Intensity" parameter by default, so don't explicitly create one.
                // We do it this way so you can have a reference to a Snapshot and then set its intensity, even though
                // we don't know 100% sure that the selected Snapshot supports that. Otherwise it's very tedious to set
                // the intensity parameter on some generic Snapshot. You would have to see if it is one of the known
                // types that has the parameter, defeating the purpose of allowing users to select one via the inspector
                if (e is FmodSnapshotEventDefinition && string.Equals(
                        parameter.FilteredName, "Intensity", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                validParameterCount++;
                
                // Define a new local parameter for this event.
                eventParametersCode += GetParameterCode(eventParameterGenerator, parameter);

                // Also cache this for generating custom play methods with all the parameters in it, it's to support
                // a syntax like 'Events.Footstep.Play(SurfaceValues.Dirt)' where the Play method has strongly-typed
                // arguments for all of its parameters.
                string parameterName = parameter.FilteredName;
                string argumentName = parameter.ArgumentName;
                string argumentType = parameter.ArgumentType;
                string argumentTypeFullyQualified = parameter.ArgumentTypeFullyQualified;
                string spacing = i == 0 ? string.Empty : ", ";
                parameterArguments += spacing + $"{argumentName}";
                parameterArgumentsWithType += spacing + $"{argumentType} {argumentName}";
                parameterArgumentsWithTypeFullyQualified +=
                    spacing + $"{argumentTypeFullyQualified} {argumentName}";
                parameterInitializationsFromArguments +=
                    $"{parameterName}.Value = {argumentName};\r\n";
            }
            
            // Actually no valid parameters were found. Ignore it after all.
            if (validParameterCount <= 0)
            {
                generator.RemoveKeywordLines(configPlayMethodWithParametersKeyword);
                generator.RemoveKeywordLines(eventParametersKeyword);
                return;
            }

            // THEN write an InitializeParameters function to pass along the instance to the parameters.
            string eventParametersInitializationCode = string.Empty;
            foreach (AudioEventParameterDefinition parameter in e.Parameters)
            {
                string parameterName = parameter.FilteredName;
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
                
                generator.ReplaceKeyword(
                    configPlayMethodWithParametersKeyword,
                    eventConfigPlayMethodWithParametersGenerator.GetCode());
            }
            else
            {
                generator.RemoveKeywordLines(configPlayMethodWithParametersKeyword);
            }
            
            generator.ReplaceKeyword(eventParametersKeyword, eventParametersCode);
        }
        
        /// <summary>
        /// Gets the name of an event *as it is represented in the AudioEvents syntax*. For example, here's an event
        /// called Core/Player/Footstep for different Syntax Formats:
        /// Flat:                                   Footstep
        /// Flat (With Path Included In Name):      Core_Player_Footstep
        /// Subclasses Per Folder:                  Footstep
        /// </summary>
        private static string GetEventSyntaxName(string filteredPath)
        {
            // If specified, include the entire path as a prefix.
            if (Settings.SyntaxFormat == AudioSyntaxSettings.SyntaxFormats.FlatWithPathIncludedInName)
                return filteredPath.Replace("_", "").Replace("/", "_");
            
            return FmodSyntaxUtilities.GetFilteredNameFromPath(filteredPath);
        }
        
        private static string GetEventSyntaxName(AudioEventDefinition e)
        {
            return GetEventSyntaxName(e.GetFilteredPath(true));
        }
        
        /// <summary>
        /// Gets the path of an event *as it is represented in the AudioEvents syntax*. For example, here's an event
        /// called Core/Player/Footstep for different Syntax Formats:
        /// Flat:                                   Footstep
        /// Flat (With Path Included In Name):      Core_Player_Footstep
        /// Subclasses Per Folder:                  Core.Player.Footstep
        /// </summary>
        private static string GetEventSyntaxPath(string filteredPath)
        {
            string eventName = GetEventSyntaxName(filteredPath);
            
            if (Settings.SyntaxFormat !=
                AudioSyntaxSettings.SyntaxFormats.SubclassesPerFolder)
            {
                return eventName;
            }
            
            string eventDirectories = Path.GetDirectoryName(filteredPath);
            
            return Path.Combine(eventDirectories, eventName).ToUnityPath().Replace("/", ".");
        }

        private static string GetEventSyntaxPath(AudioEventDefinition e)
        {
            return GetEventSyntaxPath(e.GetFilteredPath(true));
        }

        private static string GetEventCode(AudioEventDefinition e, string eventName = "", string attribute = "")
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

        [MenuItem("FMOD/Generate Audio Code %&g", false, 999999999)]
        [MenuItem(AudioSyntaxMenuPaths.Root + "Generate Audio Code", false, 999999999)]
        public static void GenerateCode()
        {
            ParseMetaData();
            
            RemoveDeprecatedGeneratedScripts();

            if (Settings.ShouldGenerateAssemblyDefinition)
                GenerateAssemblyDefinition();
            
            detectedEventChanges.Clear();
            
            // Organize the events in a folder hierarchy.
            rootEventFolder = new(EventContainerClass);
            List<AudioEventDefinition> eventDefinitions = new();
#if FMOD_AUDIO_SYNTAX
            GetFmodEvents(eventDefinitions, EditorEventRefExtensions.EventPrefix, false);
#endif // FMOD_AUDIO_SYNTAX
#if UNITY_AUDIO_SYNTAX
            GetUnityEvents(eventDefinitions);
#endif // UNITY_AUDIO_SYNTAX
            BuildEventsHierarchy(rootEventFolder, eventDefinitions);
            
            GenerateEventsScript(true, EventsScriptPath, eventsScriptGenerator, eventTypeGenerator, EventContainerClass);
            GenerateEventsScript(false, EventTypesScriptPath, eventTypesScriptGenerator, eventTypeGenerator, EventContainerClass);

#if FMOD_AUDIO_SYNTAX
            // Also build separate event files for FMOD snapshots. This is a special kind of event that can tweak
            // various mixing settings. It's an FMOD-specific concept, there are currently no plans to do an equivalent
            // for the Unity Audio Syntax implementation so this whole block can be FMOD-specific code.
            rootEventFolder = new(SnapshotContainerClass);
            eventDefinitions.Clear();
            GetFmodEvents(eventDefinitions, EditorEventRefExtensions.SnapshotPrefix, true);
            BuildEventsHierarchy(rootEventFolder, eventDefinitions);
            
            GenerateEventsScript(true, SnapshotsScriptPath, snapshotsScriptGenerator, snapshotTypeGenerator, SnapshotContainerClass);
            GenerateEventsScript(false, SnapshotTypesScriptPath, snapshotTypesScriptGenerator, snapshotTypeGenerator, SnapshotContainerClass);
#endif // FMOD_AUDIO_SYNTAX
            
            // NOTE: This re-uses the using directives from the generated events. Therefore this should be called after
            // the events are generated.
            GenerateGlobalParametersScript();
            
            GenerateMiscellaneousScripts();

            StorePreviousMetaData();

            if (detectedEventChanges.Count > 0)
                TryRefactoringOldEventReferencesInternal(false);
        }

        private static void RemoveDeprecatedGeneratedScripts()
        {
            for (int i = 0; i < DeprecatedGeneratedScriptPaths.Length; i++)
            {
                RemoveFileIncludingMetaFile(DeprecatedGeneratedScriptPaths[i]);
            }
        }

        private static void RemoveFileIncludingMetaFile(string path)
        {
            path = path.AddAssetsPrefix();
            
            if (AssetDatabase.AssetPathExists(path))
                AssetDatabase.DeleteAsset(path);
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
            
#if FMOD_AUDIO_SYNTAX
            foreach (EditorParamRef fmodParameter in EventManager.Parameters)
            {
                FmodAudioEventParameterDefinition globalParameter = new(fmodParameter, null);
                globalParametersCode += GetParameterCode(globalParameterGenerator, globalParameter);
            }
#endif // FMOD_AUDIO_SYNTAX
            
            globalParametersGenerator.Reset();
            globalParametersGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);
            
            // NOTE: This re-uses the using directives from the generated events. Therefore this should be called after
            // the events are generated.
            globalParametersGenerator.ReplaceKeyword("UsingDirectives", eventUsingDirectivesCode);
            
            globalParametersGenerator.ReplaceKeyword("GlobalParameters", globalParametersCode);
            globalParametersGenerator.GenerateFile(GlobalParametersScriptPath);
        }
        
        private static void BuildEventsHierarchy(EventFolder root, List<AudioEventDefinition> events)
        {
            // Organize the events into a folder hierarchy.
            foreach (AudioEventDefinition eventDefinition in events)
            {
                string path = eventDefinition.GetFilteredPath(true);

                EventFolder folder = root;
                
                if (Settings.SyntaxFormat == AudioSyntaxSettings.SyntaxFormats.SubclassesPerFolder)
                {
                    folder = root.GetOrCreateChildFolderFromPathRecursively(path);
                }

                folder.ChildEvents.Add(eventDefinition);
                folder.ChildEvents.Sort((x, y) => String.Compare(x.Path, y.Path, StringComparison.Ordinal));
                
                string currentPath = path;
                
                bool eventExistedDuringPreviousCodeGeneration = metaDataFromPreviousCodeGeneration
                    .EventGuidToPreviousSyntaxPath.TryGetValue(eventDefinition.Guid, out string previousSyntaxPath);
                bool shouldGenerateAlias = false;
                if (eventExistedDuringPreviousCodeGeneration)
                {
                    string currentSyntaxPath = GetEventSyntaxPath(currentPath);
                    shouldGenerateAlias = previousSyntaxPath != currentSyntaxPath;
                    
                    // Keep track of the event change we detected so we can try to automatically refactor existing
                    // code to reference the new path instead.
                    if (shouldGenerateAlias)
                        detectedEventChanges[previousSyntaxPath] = currentSyntaxPath;
                }
                
                // Also generate aliases for this event if it has been renamed so you have a chance to update the
                // code without it breaking. Outputs some nice warnings instead via an Obsolete attribute.
                if (shouldGenerateAlias)
                {
                    EventFolder previousFolder = root
                        .GetOrCreateChildFolderFromPathRecursively(previousSyntaxPath.Replace(".", "/"));

                    previousFolder.ChildEventToAliasPath[eventDefinition] = previousSyntaxPath;
                }
            }
        }

#if FMOD_AUDIO_SYNTAX
        private static void GetFmodEvents(List<AudioEventDefinition> eventDefinitions, string eventPrefix, bool isSnapshots)
        {
            EditorEventRef[] events = EventManager.Events
                .Where(e => e.Path.StartsWith(eventPrefix))
                .OrderBy(e => e.Path).ToArray();

            // Organize the events in a folder hierarchy.
            foreach (EditorEventRef e in events)
            {
                FmodEventDefinition eventDefinition;
                if (isSnapshots)
                    eventDefinition = new FmodSnapshotEventDefinition(e);
                else
                    eventDefinition = new FmodAudioEventDefinition(e);
                
                eventDefinitions.Add(eventDefinition);
            }
        }
#endif // FMOD_AUDIO_SYNTAX
        
#if UNITY_AUDIO_SYNTAX
        private static void GetUnityEvents(List<AudioEventDefinition> eventDefinitions)
        {
            UnityAudioEventConfigBase[] configs = AssetLoading.GetAllAssetsOfType<UnityAudioEventConfigBase>();

            // Organize the events in a folder hierarchy.
            foreach (UnityAudioEventConfigBase config in configs)
            {
                UnityAudioEventDefinition eventDefinition = new(config);
                
                eventDefinitions.Add(eventDefinition);
            }
        }
#endif // UNITY_AUDIO_SYNTAX

        private static void GenerateEventsScript(bool isFields, string eventsScriptPath,
            CodeGenerator codeGenerator, CodeGenerator typeGenerator, string containerName)
        {
            eventUsingDirectives.Clear();
            eventUsingDirectives.AddRange(eventUsingDirectivesDefault);

            // We either only declare the event fields or we define the events types. Separating this out into separate
            // files makes it easier to just have a look at which events were generated at all.
            //CodeGenerator codeGenerator = isFields ? eventsScriptGenerator : eventTypesScriptGenerator;
            codeGenerator.Reset();
            
            codeGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);
            
            // Generate code for the events per folder.
            parameterlessEventsCode = "";
            string eventsCode;
            if (!isFields)
            {
                // If we don't separate events with folders then we define the event types outside of the root folder,
                // which is how it used to work and prevents you from having to type
                // 'AudioEvents.NameOfEventPlayback playback;' and lets you type
                // 'NameOfEventPlayback playback;' instead (without the 'AudioEvents.')
                eventsCode = GenerateFolderCode(
                    typeGenerator, containerName, rootEventFolder, isFields, out string eventTypesCodeToBePlacedOutsideOfRootFolder);
                eventsCode = eventTypesCodeToBePlacedOutsideOfRootFolder + eventsCode;
            }
            else
            {
                eventsCode = GenerateFolderCode(typeGenerator, containerName, rootEventFolder, isFields);
            }
            codeGenerator.ReplaceKeyword("Events", eventsCode, true);
            
            if (isFields)
            {
                codeGenerator.ReplaceKeyword("ParameterlessEventIds", parameterlessEventsCode, true);

                // Write new metadata to the file while with the current data while preserving the old one,
                // because we may still need that one for generating other files.
                string versionNumber = GetCurrentVersionNumber();
                AudioSyntaxSettings.SyntaxFormats syntaxFormat = Settings.SyntaxFormat;
                MetaData newMetaData = new(versionNumber, syntaxFormat, activeEventGuidToCurrentSyntaxPath);
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

        private static string DisableWarnings(string code, string header = null)
        {
            if (string.IsNullOrEmpty(code))
                return code;
            
            if (header == null)
                header = string.Empty;
            
            return header
                   + "#pragma warning disable 618\r\n"
                   + code
                   + "#pragma warning restore 618\r\n";
        }

        private static string GenerateFolderCode(CodeGenerator typeGenerator, string containerName,
            EventFolder eventFolder, bool isDeclaration, out string eventTypesCodeToBePlacedOutsideOfRootFolder)
        {
            string childFoldersCode = "";
            for (int i = 0; i < eventFolder.ChildFolders.Count; i++)
            {
                EventFolder childFolder = eventFolder.ChildFolders[i];
                string childFolderCode = GenerateFolderCode(typeGenerator, containerName, childFolder, isDeclaration);
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
            string eventAliasesCode = "";
            string eventTypesCode = string.Empty;
            string eventsCode = string.Empty;
            foreach (AudioEventDefinition e in eventFolder.ChildEvents)
            {
                // Log it in the active event GUIDs. This way we can keep track of which events we had previously and
                // which paths / GUIDs they had. Then we can figure out if they were renamed/moved, then add those
                // back with an alias and an Obsolete tag so everything doesn't break immediately and you get a chance
                // to fix your code first.
                // NOTE: We don't *have* to keep track of them this way necessarily, we could intercept UpdateCache
                // in EventManager.cs and make it expose a list of renamed events. Would require changing FMOD even
                // further though, and the changes are already stacking up...
                if (isDeclaration)
                    activeEventGuidToCurrentSyntaxPath[e.Guid] = GetEventSyntaxPath(e);

                // Types
                if (!isDeclaration)
                    eventTypesCode += GetEventTypeCode(typeGenerator, e);

                // Fields
                if (isDeclaration)
                    eventsCode += GetEventCode(e);

                if (e.IsParameterless)
                {
                    parameterlessEventsCode += $"{{ \"{e.Guid}\", new FmodParameterlessAudioConfig(\"{e.Guid}\") }},\r\n";
                }
            }

            // Generate code for event aliases.
            if (Settings.GenerateFallbacksForChangedEvents)
            {
                AudioSyntaxSettings.SyntaxFormats previousSyntaxFormat = metaDataFromPreviousCodeGeneration.SyntaxFormat;
                AudioSyntaxSettings.SyntaxFormats currentSyntaxFormat = Settings.SyntaxFormat;
                
                foreach (KeyValuePair<AudioEventDefinition, string> eventPreviousPathPair in
                         eventFolder.ChildEventToAliasPath)
                {
                    AudioEventDefinition e = eventPreviousPathPair.Key;
                    string currentSyntaxPath = GetEventSyntaxPath(e);
                    string previousSyntaxPath = eventPreviousPathPair.Value;

                    string previousName = Path.GetFileName(previousSyntaxPath.Replace(".", "/"));

                    if (isDeclaration)
                    {
                        string previousEventFieldName = GetEventField(previousSyntaxPath, containerName);
                        string currentEventFieldName = GetEventField(currentSyntaxPath, containerName);
                        string attribute =
                            $"[Obsolete(\"FMOD Event '{previousEventFieldName}' has been changed to '{currentEventFieldName}'\")]";
                        eventAliasesCode += GetEventCode(e, previousName, attribute);
                    }
                    else
                    {
                        string previousEventPlaybackType = GetEventPlaybackType(previousSyntaxPath, previousSyntaxFormat);
                        string currentEventPlaybackType = GetEventPlaybackType(currentSyntaxPath, currentSyntaxFormat);
                        string attribute =
                            $"[Obsolete(\"FMOD Event Type '{previousEventPlaybackType}' has been changed to '{currentEventPlaybackType}'\")]";
                        string eventTypeAliasCode = GetEventTypeCode(typeGenerator, e, previousName, attribute);
                        eventTypeAliasesCode += eventTypeAliasCode;
                    }
                }
            }

            // Also add a section for any event type aliases, if needed.
            const string eventTypeAliasesKeyword = "EventTypeAliases";
            
            // NOTE: We disable obsolete warnings for the classes themselves, as technically the Config class
            // is using the obsolete Playback class, but we only want obsolete warnings where it is ACTUALLY
            // used in the codebase by developers.
            const string eventTypeAliasesHeader = "\r\n// Aliases for event types that have been renamed:\r\n";
            eventTypeAliasesCode = DisableWarnings(eventTypeAliasesCode, eventTypeAliasesHeader);

            // If we separate events with folders then we define the types inside the folder in question. Otherwise we
            // only have one root folder, and we define the types outside of that, which is how it used to work and
            // prevents you from having to type 'AudioEvents.NameOfEventPlayback playback;' and lets you type
            // 'NameOfEventPlayback playback;' instead, without the 'AudioEvents.'
            eventTypesCodeToBePlacedOutsideOfRootFolder = string.Empty;
            const string eventTypesKeyword = "EventTypes";
            if (Settings.SyntaxFormat == AudioSyntaxSettings.SyntaxFormats.SubclassesPerFolder)
            {
                eventFolderGenerator.ReplaceKeyword(eventTypesKeyword, eventTypesCode, true);
            }
            else
            {
                eventFolderGenerator.RemoveKeywordLines(eventTypesKeyword);
                eventTypesCodeToBePlacedOutsideOfRootFolder = eventTypesCode;
            }
            
            // Also decide where to place the event type aliases, which should either be in the appropriate folder
            // or *next* to the AudioEvents class for folderless syntaxes.
            if (metaDataFromPreviousCodeGeneration.SyntaxFormat ==
                AudioSyntaxSettings.SyntaxFormats.SubclassesPerFolder)
            {
                // Previously we did use folders, so place the type aliases in the appropriate folder.
                eventFolderGenerator.ReplaceKeyword(eventTypeAliasesKeyword, eventTypeAliasesCode);
            }
            else
            {
                // Most event type aliases code should go inside the appropriate folder, but if code was previously
                // generated with a folderless syntax format then those type aliases should be placed OUTSIDE
                // of the root folder because that's where the types in question used to be declared.
                eventFolderGenerator.RemoveKeywordLines(eventTypeAliasesKeyword);
                if (!string.IsNullOrEmpty(eventTypeAliasesCode))
                    eventTypesCodeToBePlacedOutsideOfRootFolder += "\r\n" + eventTypeAliasesCode + "\r\n";
            }
            
            eventFolderGenerator.ReplaceKeyword("Events", eventsCode, true);
            
            // Also add a section for any event aliases, if needed.
            const string eventAliasesKeyword = "EventAliases";
            if (string.IsNullOrEmpty(eventAliasesCode) || !Settings.GenerateFallbacksForChangedEvents)
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

        private static string GenerateFolderCode(
            CodeGenerator typeGenerator, string containerName, EventFolder eventFolder, bool isDeclaration)
        {
            return GenerateFolderCode(typeGenerator, containerName, eventFolder, isDeclaration, out string _);
        }
    }
}
