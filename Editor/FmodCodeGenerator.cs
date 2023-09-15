using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Linq;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace RoyTheunissen.FMODWrapper
{
    /// <summary>
    /// Generates code for FMOD events & parameters.
    /// </summary>
    public static class FmodCodeGenerator
    {
        private static string ScriptPathBase => FmodWrapperSettings.Instance.GeneratedScriptsFolderPath;
        private const string TemplatePathBase = "Templates/Fmod/";
        
        private static string EventsScriptPath => ScriptPathBase + "FmodEvents.cs";
        private const string EventsTemplatePath = TemplatePathBase + "Events/";
        
        private static string BanksScriptPath => ScriptPathBase + "FmodBanks.cs";
        private const string BanksTemplatePath = TemplatePathBase + "Banks/";
        
        private static string BusesScriptPath => ScriptPathBase + "FmodBuses.cs";
        private const string BusesTemplatePath = TemplatePathBase + "Buses/";
        
        private static readonly CodeGenerator assemblyDefinitionGenerator =
            new CodeGenerator(TemplatePathBase + "FMOD-Wrapper.asmdef");

        private const string EventNameKeyword = "EventName";
        private static readonly CodeGenerator eventsScriptGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEvents.cs");
        private static readonly CodeGenerator eventTypesGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventTypes.cs");
        private static readonly CodeGenerator eventFieldsGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventFields.cs");
        private static readonly CodeGenerator eventParameterGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventParameter.cs");
        private static readonly CodeGenerator eventParametersInitializationGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventParametersInitialization.cs");
        private static readonly CodeGenerator eventConfigPlayMethodWithParametersGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventConfigPlayMethodWithParameters.cs");
        private static readonly CodeGenerator eventPlaybackPlayMethodWithParametersGenerator =
            new CodeGenerator(EventsTemplatePath + "FmodEventPlaybackPlayMethodWithParameters.cs");

        private static readonly CodeGenerator enumGenerator = new CodeGenerator(EventsTemplatePath + "FmodEnum.cs");
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
        
        private static FmodWrapperSettings Settings => FmodWrapperSettings.Instance;
        
        [NonSerialized] private static List<string> eventUsingDirectives = new List<string>();
        [NonSerialized] private static string[] eventUsingDirectivesDefault =
        {
            "System.Collections.Generic",
            "FMOD.Studio",
            "RoyTheunissen.FMODWrapper",
            "UnityEngine",
            "UnityEngine.Scripting",
        };

        [NonSerialized] private static bool didSourceFilesChange;

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

        private static readonly Dictionary<string, Type> labelParameterNameToUserSpecifiedEnumType 
            = new Dictionary<string, Type>();

        private static bool didCacheUserSpecifiedEnums;

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

        public static bool HasUserSpecifiedLabelParameterEnum(string name)
        {
            CacheUserSpecifiedLabelParameterEnums();
            
            return labelParameterNameToUserSpecifiedEnumType.ContainsKey(name);
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
                    // Make sure we have an appropriate using directive.
                    string usingDirectiveForEnum = enumType.Namespace;
                    if (!eventUsingDirectives.Contains(usingDirectiveForEnum))
                        eventUsingDirectives.Add(usingDirectiveForEnum);
                    
                    // Not generating a new enum.
                    codeGenerator.RemoveKeywordLines(enumKeyword);
                }
                else
                {
                    string enumValues = string.Empty;
                    for (int i = 0; i < parameter.Labels.Length; i++)
                    {
                        enumValues += $"{parameter.Labels[i]}";
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

        private static Dictionary<string, string> GetExistingEventNamesByGuid()
        {
            Dictionary<string, string> existingEventNamesByGuid = new Dictionary<string, string>();
            
            // If a script has already been generated, open it.
            string existingFilePath = EventsScriptPath.GetAbsolutePath();
            if (!File.Exists(existingFilePath))
                return existingEventNamesByGuid;
            
            // Check that there's a section with existing events by GUID.
            string existingCode = File.ReadAllText(existingFilePath);
            string activeEventGuidsSection = existingCode.GetSection(
                "/* ACTIVE EVENT GUIDS", "ACTIVE EVENT GUIDS */");
            if (string.IsNullOrEmpty(activeEventGuidsSection))
                return existingEventNamesByGuid;
            
            // Every line is an individual event formatted as name=guid
            string[] lines = activeEventGuidsSection.Split("\r\n");
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].TrimStart();
                string[] nameAndGuid = line.Split("=");
                if (nameAndGuid.Length != 2)
                    continue;
                        
                string name = nameAndGuid[0];
                string guid = nameAndGuid[1];
                        
                existingEventNamesByGuid.Add(guid, name);
            }

            return existingEventNamesByGuid;
        }
        
        private static string GetEventTypeCode(EditorEventRef e, string eventName = "", string attribute = "")
        {
            if (string.IsNullOrEmpty(eventName))
                eventName = e.GetFilteredName();
            
            eventTypesGenerator.Reset();
            eventTypesGenerator.ReplaceKeyword(EventNameKeyword, eventName);
            
            // Parameters
            GetEventTypeParametersCode(e, eventName);

            const string attributesKeyword = "Attributes";
            if (string.IsNullOrEmpty(attribute))
                eventTypesGenerator.RemoveKeywordLines(attributesKeyword);
            else
                eventTypesGenerator.ReplaceKeyword(attributesKeyword, attribute);
            
            return eventTypesGenerator.GetCode();
        }

        private static void GetEventTypeParametersCode(EditorEventRef e, string eventName)
        {
            const string eventParametersKeyword = "EventParameters";
            const string configPlayMethodWithParametersKeyword = "ConfigPlayMethodWithParameters";
            
            // If there's no parameters for this event then we can just get rid of the keywords and leave it there.
            if (e.LocalParameters.Count <= 0)
            {
                eventTypesGenerator.RemoveKeywordLines(configPlayMethodWithParametersKeyword);
                eventTypesGenerator.RemoveKeywordLines(eventParametersKeyword);
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
                
                eventTypesGenerator.ReplaceKeyword(
                    configPlayMethodWithParametersKeyword,
                    eventConfigPlayMethodWithParametersGenerator.GetCode());
            }
            else
            {
                eventTypesGenerator.RemoveKeywordLines(configPlayMethodWithParametersKeyword);
            }
            
            eventTypesGenerator.ReplaceKeyword(eventParametersKeyword, eventParametersCode);
        }

        private static string GetEventCode(EditorEventRef e, string eventName = "", string attribute = "")
        {
            // By default we use the event's own name, but when we're generating aliases we actually want to generate
            // a Config/Playback that has an old name but points to the GUID of the newly named event, so we want to 
            // be able to specify a different name in that use case.
            if (string.IsNullOrEmpty(eventName))
                eventName = e.GetFilteredName();
            string fieldName = FmodWrapperUtilities.GetFilteredNameFromPathLowerCase(eventName);
            
            eventFieldsGenerator.Reset();
            eventFieldsGenerator.ReplaceKeyword(EventNameKeyword, eventName);
            eventFieldsGenerator.ReplaceKeyword("eventName", fieldName);
            eventFieldsGenerator.ReplaceKeyword("GUID", e.Guid.ToString());
            
            // Aliases have an Obsolete attribute, normal events don't and can just remove the keyword.
            if (string.IsNullOrEmpty(attribute))
                eventFieldsGenerator.RemoveKeywordLines("Attributes");
            else
                eventFieldsGenerator.ReplaceKeyword("Attributes", attribute);
            
            return eventFieldsGenerator.GetCode();
        }

        [MenuItem("FMOD/Generate FMOD Code %&g", false, 999999999)]
        private static void GenerateCode()
        {
            if (Settings.ShouldGenerateAssemblyDefinition)
                GenerateAssemblyDefinition();
            
            GenerateEventsScript();
            GenerateBanksAndBusesScripts();
        }

        private static void GenerateAssemblyDefinition()
        {
            assemblyDefinitionGenerator.Reset();
            
            assemblyDefinitionGenerator.ReplaceKeyword("Name", Settings.NamespaceForGeneratedCode);
            assemblyDefinitionGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);
            
            assemblyDefinitionGenerator.GenerateFile(ScriptPathBase + $"{Settings.NamespaceForGeneratedCode}.asmdef");
        }

        private static void GenerateEventsScript()
        {
            eventUsingDirectives.Clear();
            eventUsingDirectives.AddRange(eventUsingDirectivesDefault);

            eventsScriptGenerator.Reset();
            
            eventsScriptGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);

            Dictionary<string, string> previousEventNamesByGuid = GetExistingEventNamesByGuid();

            // Generate a config & playback class for every FMOD event.
            string activeEventGuidsCode = string.Empty;
            string eventTypesCode = string.Empty;
            string eventsCode = string.Empty;
            EditorEventRef[] events = EventManager.Events
                .Where(e => e.Path.StartsWith(EditorEventRefExtensions.EventPrefix))
                .OrderBy(e => e.Path).ToArray();
            string eventTypeAliasesCode = "";
            string eventAliasesCode = "";
            string parameterlessEventsCode = "";
            foreach (EditorEventRef e in events)
            {
                string eventName = e.GetFilteredName();

                // Check if this event used to go by a different name.
                string currentName = eventName;
                bool wasRenamed = previousEventNamesByGuid.TryGetValue(e.Guid.ToString(), out string previousName) &&
                                  previousName != currentName;

                // Log it in the active event GUIDs. This way we can keep track of which events we had previously and
                // which names / GUIDs they had. Then we can figure out if any of them have renamed, then add those
                // back with an alias and an Obsolete tag so everything doesn't break immediately and you get a chance
                // to fix your code first.
                // NOTE: We don't *have* to keep track of them this way necessarily, we could intercept UpdateCache
                // in EventManager.cs and make it expose a list of renamed events. Would require changing FMOD even
                // further though, and the changes are already stacking up...
                activeEventGuidsCode += $"{eventName}={e.Guid}\r\n";

                // Types
                eventTypesCode += GetEventTypeCode(e);

                // Fields
                eventsCode += GetEventCode(e);

                if (e.LocalParameters.Count == 0)
                {
                    parameterlessEventsCode += $"{{ \"{e.Guid}\", new FmodParameterlessAudioConfig(\"{e.Guid}\") }},\r\n";
                }

                // Also generate aliases for this event if it has been renamed so you have a chance to update the
                // code without it breaking. Outputs some nice warnings instead via an Obsolete attribute.
                if (wasRenamed)
                {
                    string attribute = $"[Obsolete(\"FMOD Event '{previousName}' has been renamed to '{currentName}'\")]";

                    eventTypeAliasesCode += GetEventTypeCode(e, previousName, attribute);
                    eventAliasesCode += GetEventCode(e, previousName, attribute);
                }
            }

            // Also add a section for any event type aliases, if needed.
            const string eventTypeAliasesKeyword = "EventTypeAliases";
            if (string.IsNullOrEmpty(eventTypeAliasesCode))
            {
                eventsScriptGenerator.RemoveKeywordLines(eventTypeAliasesKeyword);
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
                eventsScriptGenerator.ReplaceKeyword(eventTypeAliasesKeyword, eventTypeAliasesCode);
            }
            
            eventsScriptGenerator.ReplaceKeyword("ParameterlessEventIds", parameterlessEventsCode, true);
            eventsScriptGenerator.ReplaceKeyword("ActiveEventGuids", activeEventGuidsCode);
            eventsScriptGenerator.ReplaceKeyword("EventTypes", eventTypesCode);
            eventsScriptGenerator.ReplaceKeyword("Events", eventsCode);

            // Also add a section for any event aliases, if needed.
            const string eventAliasesKeyword = "EventAliases";
            if (string.IsNullOrEmpty(eventAliasesCode))
            {
                eventsScriptGenerator.RemoveKeywordLines(eventAliasesKeyword);
            }
            else
            {
                eventAliasesCode = "\r\n// Aliases for events that have been renamed:\r\n" + eventAliasesCode;
                eventsScriptGenerator.ReplaceKeyword(eventAliasesKeyword, eventAliasesCode);
            }

            // Also generate a field for every FMOD global parameter.
            string globalParametersCode = string.Empty;
            foreach (EditorParamRef parameter in EventManager.Parameters)
            {
                globalParametersCode += GetParameterCode(globalParameterGenerator, parameter);
            }

            eventsScriptGenerator.ReplaceKeyword("GlobalParameters", globalParametersCode);

            // Also allow custom using directives to be specified.
            string usingDirectives = string.Empty;
            for (int i = 0; i < eventUsingDirectives.Count; i++)
            {
                string usingDirective = eventUsingDirectives[i];
                usingDirectives += $"using {usingDirective};\r\n";
            }
            eventsScriptGenerator.ReplaceKeyword("UsingDirectives", usingDirectives);

            eventsScriptGenerator.GenerateFile(EventsScriptPath);
        }

        private static void GenerateBanksAndBusesScripts()
        {
            banksScriptGenerator.Reset();
            
            banksScriptGenerator.ReplaceKeyword("Namespace", Settings.NamespaceForGeneratedCode);
            
            string banksCode = string.Empty;

            // Need to access the banks this way, not via EventManager.Banks because EditorBankRef doesn't have info
            // on the buses, and we need that for the sake of generating bus code.
            EditorUtils.LoadPreviewBanks();
            EditorUtils.System.getBankList(out Bank[] banks);
            
            banks = banks.OrderBy(b => b.getPath()).ToArray();
            List<FMOD.Studio.Bus> buses = new List<FMOD.Studio.Bus>();
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
        }
    }
}
