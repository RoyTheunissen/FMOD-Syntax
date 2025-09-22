using System;
using System.Collections.Generic;

namespace RoyTheunissen.AudioSyntax
{
    public sealed class MigrationFmodSyntaxToAudioSyntax : Migration
    {
        public override int VersionMigratingTo => 1;

        public override string DisplayName => "FMOD-Syntax to Audio-Syntax";

        public override string Description => "The system has since been updated to support Unity-based audio as well, " +
                                              "and has been renamed from FMOD-Syntax to Audio-Syntax. Certain " +
                                              "namespaces / classes have been renamed, we need to make sure those " +
                                              "are now updated if necessary.";

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
            bool isNecessary = IsReplacementNecessary(FmodSyntaxNamespace, AudioSyntaxNamespace);

            urgency = Migration.IssueUrgencies.Required;

            return isNecessary;
        }

        protected override void OnPerform()
        {
            ReplaceInScripts(FmodSyntaxNamespace, AudioSyntaxNamespace);
        }
    }

    public sealed class FmodSyntaxOutdatedSystemReferencesRefactor : FmodSyntaxToAudioSyntaxRefactor
    {
        private readonly Dictionary<string, string> outdatedSystemReferenceReplacements = new()
        {
            { $"{FmodSyntaxSystemName}.{CullPlaybacksMethod}", $"{GeneralSystemName}.{UpdateMethod}" },
            { $"{FmodSyntaxSystemName}.{StopAllActivePlaybacksMethod}",
                $"{GeneralSystemName}.{StopAllActivePlaybacksMethod}" },
            { $"{FmodSyntaxSystemName}.{StopAllActiveEventPlaybacksMethod}",
                $"{GeneralSystemName}.{StopAllActiveEventPlaybacksMethod}" },
        };
        
        [NonSerialized] private string cachedOutdatedSystemReferencesDisplayText;
        [NonSerialized] private bool didCacheOutdatedSystemReferencesDisplayText;
        private string OutdatedSystemReferencesDisplayText
        {
            get
            {
                if (!didCacheOutdatedSystemReferencesDisplayText)
                {
                    didCacheOutdatedSystemReferencesDisplayText = true;
                    cachedOutdatedSystemReferencesDisplayText =
                        GetDisplayTextForReplacements(outdatedSystemReferenceReplacements);
                }
                return cachedOutdatedSystemReferencesDisplayText;
            }
        }

        protected override string IsNecessaryDisplayText => $"There used to be one system called '{FmodSyntaxSystemName}'. This has " +
                                                            $"been replaced by a general system '{GeneralSystemName}' which in turn " +
                                                            $"updates both '{FmodSyntaxSystemName}' and '{UnityAudioSystemName}'. " +
                                                            $"The '{CullPlaybacksMethod}' method has also been renamed " +
                                                            $"to '{UpdateMethod}' because it now does more than just culling " +
                                                            $"playbacks.\n\n" + OutdatedSystemReferencesDisplayText;

        protected override string NotNecessaryDisplayText => $"There seem to be no more outdated references to '{FmodSyntaxSystemName}'.";

        protected override string ConfirmationDialogueText => $"Are you sure you want to automatically update references to the old system " +
                                                              $"'{FmodSyntaxSystemName}' with references to the new system '{GeneralSystemName}' " +
                                                              $"where possible?";

        protected override bool CheckIfNecessaryInternal(out Migration.IssueUrgencies urgency)
        {
            bool isNecessary = AreReplacementsNecessary(outdatedSystemReferenceReplacements);

            urgency = Migration.IssueUrgencies.Optional;
            
            return isNecessary;
        }

        protected override void OnPerform()
        {
            ReplaceInScripts(outdatedSystemReferenceReplacements);
        }
    }
    
    public sealed class FmodSyntaxAudioReferencePlaybackTypeRefactor : FmodSyntaxToAudioSyntaxRefactor
    {
        private const string OldPlaybackType = "FmodParameterlessAudioPlayback";
        private const string NewPlaybackType = "IAudioPlayback";

        protected override string IsNecessaryDisplayText => $"Playing an AudioReference assignable via the inspector used to return an instance of '{OldPlaybackType}' but given that it now supports Unity native audio as well, it now returns a '{NewPlaybackType}' instead.";

        protected override string NotNecessaryDisplayText => $"There seem to be no more outdated references to '{OldPlaybackType}'.";

        protected override string ConfirmationDialogueText => $"Are you sure you want to automatically update references to " +
                                                              $"'{OldPlaybackType}' with references to '{NewPlaybackType}' " +
                                                              $"where possible?";

        protected override bool CheckIfNecessaryInternal(out Migration.IssueUrgencies urgency)
        {
            bool isNecessary = IsReplacementNecessary(OldPlaybackType, NewPlaybackType);

            urgency = Migration.IssueUrgencies.Required;
            
            return isNecessary;
        }

        protected override void OnPerform()
        {
            ReplaceInScripts(OldPlaybackType, NewPlaybackType);
        }
    }
}
