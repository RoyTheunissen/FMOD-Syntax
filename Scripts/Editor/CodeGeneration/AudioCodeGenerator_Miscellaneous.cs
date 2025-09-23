using System;
using System.Collections.Generic;
using System.Linq;
using FMOD.Studio;
using FMODUnity;

namespace RoyTheunissen.AudioSyntax
{
    public static partial class AudioCodeGenerator
    {
        private static string BanksScriptPath => ScriptPathBase + "AudioBanks.g.cs";
        private const string BanksTemplatePath = TemplatePathBase + "Banks/";
        private static string BusesScriptPath => ScriptPathBase + "AudioBuses.g.cs";
        private const string BusesTemplatePath = TemplatePathBase + "Buses/";
        private static string VCAsScriptPath => ScriptPathBase + "AudioVCAs.g.cs";
        private const string VCAsTemplatePath = TemplatePathBase + "VCAs/";

        private static readonly CodeGenerator assemblyDefinitionGenerator =
            new(TemplatePathBase + "Audio-Syntax.asmdef");

        private static readonly CodeGenerator bankFieldGenerator =
            new(BanksTemplatePath + "AudioBankField.g.cs");

        private static readonly CodeGenerator busesScriptGenerator =
            new(BusesTemplatePath + "AudioBuses.g.cs");

        private static readonly CodeGenerator busFieldGenerator =
            new(BusesTemplatePath + "AudioBusField.g.cs");

        private static readonly CodeGenerator vcasScriptGenerator =
            new(VCAsTemplatePath + "AudioVCAs.g.cs");

        private static readonly CodeGenerator vcaFieldGenerator =
            new(VCAsTemplatePath + "AudioVCAField.g.cs");

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
            List<FMOD.Studio.Bus> buses = new();
            List<FMOD.Studio.VCA> VCAs = new();
            foreach (Bank bank in banks)
            {
                bankFieldGenerator.Reset();
                string bankPath = bank.getPath();
                string bankName = bank.GetName();

                if (bankPath.Contains("."))
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
            List<string> combinedDictionarySectionLines = new();
            
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
    }
}
