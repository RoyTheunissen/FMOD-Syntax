using System;
using System.Collections.Generic;

namespace RoyTheunissen.AudioSyntax
{
    public sealed class MigrationFmodSyntaxToAudioSyntax : Migration
    {
        public override int VersionMigratingTo => 1;

        public override string DisplayName => "FMOD-Syntax to Audio-Syntax";

        public override string Description => "The system has since been updated to support Unity-based audio as " +
                                              "well, and has been renamed from FMOD-Syntax to Audio-Syntax " +
                                              "accordingly. Certain namespaces / classes have been renamed, we need " +
                                              "to make sure those are now updated if necessary. Additionally, " +
                                              "playing audio now by default returns playback instances with audio " +
                                              "system-agnostic types.";

        public override string DocumentationURL =>
            "https://github.com/RoyTheunissen/FMOD-Syntax?tab=readme-ov-file#migration-to-audio-syntax";

        protected override void RegisterRefactors(List<Refactor> refactors)
        {
            refactors.Add(new FmodSyntaxNamespaceToAudioSyntaxNamespaceRefactor());
            refactors.Add(new FmodSyntaxOutdatedSystemReferencesRefactor());
            refactors.Add(new FmodSyntaxAudioReferencePlaybackTypeRefactor());
        }
    }

    public abstract class FmodSyntaxToAudioSyntaxRefactor : Refactor
    {
        protected const string FmodSyntaxNamespace = "RoyTheunissen.FMODSyntax";
        protected const string AudioSyntaxNamespace = "RoyTheunissen.AudioSyntax";

        protected const string FmodSyntaxSystemName = "FmodSyntaxSystem";
        protected const string GeneralSystemName = "AudioSyntaxSystem";
        protected const string UnityAudioSystemName = "UnityAudioSyntaxSystem";
        protected const string CullPlaybacksMethod = "CullPlaybacks";
        protected const string StopAllActivePlaybacksMethod = "StopAllActivePlaybacks";
        protected const string StopAllActiveEventPlaybacksMethod = "StopAllActiveEventPlaybacks";
        protected const string UpdateMethod = "Update";
    }

    public sealed class FmodSyntaxNamespaceToAudioSyntaxNamespaceRefactor : FmodSyntaxToAudioSyntaxRefactor
    {
        protected override string NotNecessaryDisplayText =>
            $"There seem to be no more occurrences of the deprecated " +
            $"{FmodSyntaxNamespace} namespace.";

        protected override string IsNecessaryDisplayText => $"The system has detected that the FMOD-Syntax namespace " +
                                                            $"'{FmodSyntaxNamespace}' is being used. This has since been renamed to " +
                                                            $"'{AudioSyntaxNamespace}'.";

        protected override string ConfirmationDialogueText =>
            $"Are you sure you want to automatically replace the {FmodSyntaxNamespace} namespace with the {AudioSyntaxNamespace} namespace?";

        protected override bool CheckIfNecessaryInternal(out Migration.IssueUrgencies urgency)
        {
            bool isNecessary = IsReplacementNecessary(FmodSyntaxNamespace, AudioSyntaxNamespace, FileScopes.Everything);

            urgency = Migration.IssueUrgencies.Required;

            return isNecessary;
        }

        protected override void OnPerform()
        {
            ReplaceInScripts(FmodSyntaxNamespace, AudioSyntaxNamespace, FileScopes.Everything);
        }
    }

    public sealed class FmodSyntaxOutdatedSystemReferencesRefactor : FmodSyntaxToAudioSyntaxRefactor
    {
        private readonly Dictionary<string, string> replacements = new()
        {
            { $"{FmodSyntaxSystemName}.{CullPlaybacksMethod}", $"{GeneralSystemName}.{UpdateMethod}" },
            { $"{FmodSyntaxSystemName}.{StopAllActivePlaybacksMethod}",
                $"{GeneralSystemName}.{StopAllActivePlaybacksMethod}" },
            { $"{FmodSyntaxSystemName}.{StopAllActiveEventPlaybacksMethod}",
                $"{GeneralSystemName}.{StopAllActiveEventPlaybacksMethod}" },
        };
        
        [NonSerialized] private string cachedReplacementsDisplayText;
        [NonSerialized] private bool didReplacementsDisplayText;
        private string ReplacementsDisplayText
        {
            get
            {
                if (!didReplacementsDisplayText)
                {
                    didReplacementsDisplayText = true;
                    cachedReplacementsDisplayText = GetDisplayTextForReplacements(replacements);
                }
                return cachedReplacementsDisplayText;
            }
        }

        protected override string IsNecessaryDisplayText => $"There used to be one system called '{FmodSyntaxSystemName}'. This has " +
                                                            $"been replaced by a general system '{GeneralSystemName}' which in turn " +
                                                            $"updates both '{FmodSyntaxSystemName}' and '{UnityAudioSystemName}'. " +
                                                            $"The '{CullPlaybacksMethod}' method has also been renamed " +
                                                            $"to '{UpdateMethod}' because it now does more than just culling " +
                                                            $"playbacks.\n\n" + ReplacementsDisplayText;

        protected override string NotNecessaryDisplayText => $"There seem to be no more outdated references to '{FmodSyntaxSystemName}'.";

        protected override string ConfirmationDialogueText => $"Are you sure you want to automatically update references to the old system " +
                                                              $"'{FmodSyntaxSystemName}' with references to the new system '{GeneralSystemName}' " +
                                                              $"where possible?";

        protected override bool CheckIfNecessaryInternal(out Migration.IssueUrgencies urgency)
        {
            bool isNecessary = AreReplacementsNecessary(replacements, ~FileScopes.GeneratedCode);

            urgency = Migration.IssueUrgencies.Required;
            
            return isNecessary;
        }

        protected override void OnPerform()
        {
            ReplaceInScripts(replacements, ~FileScopes.GeneratedCode);
        }
    }
    
    public sealed class FmodSyntaxAudioReferencePlaybackTypeRefactor : FmodSyntaxToAudioSyntaxRefactor
    {
        private const string OldParameterlessPlaybackType = "FmodParameterlessAudioPlayback";
        private const string OldFmodSpecificPlaybackType = "FmodAudioPlayback";
        
        private const string NewPlaybackType = "IAudioPlayback";

        protected override string IsNecessaryDisplayText => $"Playing an AudioReference assignable via the inspector used to return an instance of '{OldParameterlessPlaybackType}' but given that it now supports Unity native audio as well, it now returns an '{NewPlaybackType}' instead.\n\n" + ReplacementsDisplayText;

        protected override string NotNecessaryDisplayText => $"There seem to be no more outdated references to '{OldParameterlessPlaybackType}' / '{OldFmodSpecificPlaybackType}'.";

        protected override string ConfirmationDialogueText => $"Are you sure you want to automatically update references to " +
                                                              $"'{OldParameterlessPlaybackType}' / '{OldFmodSpecificPlaybackType}' with references to '{NewPlaybackType}' " +
                                                              $"where possible?";
        
        private readonly Dictionary<string, string> replacements = new()
        {
            { OldParameterlessPlaybackType, NewPlaybackType },
            { OldFmodSpecificPlaybackType, NewPlaybackType },
        };
        
        [NonSerialized] private string cachedReplacementsDisplayText;
        [NonSerialized] private bool didCacheReplacementsDisplayText;
        private string ReplacementsDisplayText
        {
            get
            {
                if (!didCacheReplacementsDisplayText)
                {
                    didCacheReplacementsDisplayText = true;
                    cachedReplacementsDisplayText = GetDisplayTextForReplacements(replacements);
                }
                return cachedReplacementsDisplayText;
            }
        }

        protected override bool CheckIfNecessaryInternal(out Migration.IssueUrgencies urgency)
        {
            bool isNecessary = IsReplacementNecessary(OldParameterlessPlaybackType, NewPlaybackType, ~FileScopes.GeneratedCode);

            urgency = Migration.IssueUrgencies.Required;
            
            return isNecessary;
        }

        protected override void OnPerform()
        {
            // NOTE: It's fine for generated code to generate types that inherit from FmodParameterlessAudioPlayback / FmodAudioPlayback
            ReplaceInScripts(OldParameterlessPlaybackType, NewPlaybackType, ~FileScopes.GeneratedCode);
            ReplaceInScripts(OldFmodSpecificPlaybackType, NewPlaybackType, ~FileScopes.GeneratedCode);

            // The above refactor actually should not be done for implementations of IOnFmodPlayback's methods.
            // So we will go looking for those and put those back the way they were, that's easier than trying to figure
            // out whether an occurrence of the old playback type is an IOnFmodPlayback method and then not replacing it.
            string onFmodPlaybackRegistrationMethod = "OnFmodPlaybackRegistered({0} ";
            string onFmodPlaybackUnregistrationMethod = "OnFmodPlaybackUnregistered({0} ";
            ReplaceInScripts(
                string.Format(onFmodPlaybackRegistrationMethod, NewPlaybackType),
                string.Format(onFmodPlaybackRegistrationMethod, OldFmodSpecificPlaybackType),
                ~FileScopes.GeneratedCode);
            ReplaceInScripts(
                string.Format(onFmodPlaybackUnregistrationMethod, NewPlaybackType),
                string.Format(onFmodPlaybackUnregistrationMethod, OldFmodSpecificPlaybackType),
                ~FileScopes.GeneratedCode);
        }
    }
}
