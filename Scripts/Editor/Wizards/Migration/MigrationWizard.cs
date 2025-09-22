// #define ALLOW_OPENING_AUDIO_SYNTAX_MIGRATION_WIZARD_EXPLICITLY

using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Window to show to users when they haven't set up the FMOD Syntax system yet to let them conveniently
    /// initialize it with appropriate settings.
    /// </summary>
    public sealed class MigrationWizard : WizardBase
    {
        public static readonly string MigrationNecessaryText = $"It was detected that you used an earlier version of " +
                                                     $"{AudioSyntaxMenuPaths.ProjectName} and that certain changes " +
                                                     $"need to be made before your project is in working order again.";

        private static readonly string ProgressTitle = $"Detecting {AudioSyntaxMenuPaths.ProjectName} migration status";
        private const string ProgressInfo = "In the process of checking what version you're on, and if you're behind, what changes are necessary for you to be up-to-date.";
        
        private const int Priority = 0;
        
        private const string OpenMenuPath = AudioSyntaxMenuPaths.Root + "Open Migration Wizard";
        
        private const float Width = 500;
        
        private const float ThickButtonHeight = 48;
        
        private Vector2 scrollPosition;

        private int versionMigratingFrom;
        private int versionMigratingTo;

        private bool hasDetectedIssues;
        private Migration.IssueUrgencies detectedIssueUrgency;
        
        private int refreshProgressId;

        private static readonly Migration[] Migrations = { Migration.Create<MigrationFmodSyntaxToAudioSyntax>() };
        
#if ALLOW_OPENING_AUDIO_SYNTAX_MIGRATION_WIZARD_EXPLICITLY
        [MenuItem(OpenMenuPath, false, Priority)]
#endif // ALLOW_OPENING_AUDIO_SYNTAX_MIGRATION_WIZARD_EXPLICITLY
        public static void OpenMigrationWizard()
        {
            MigrationWizard migrationWizard = GetWindow<MigrationWizard>(
                false, AudioSyntaxMenuPaths.ProjectName + " Migration Wizard");
            migrationWizard.minSize = new Vector2(Width, 150);
            migrationWizard.Refresh();
        }
        
#if ALLOW_OPENING_AUDIO_SYNTAX_MIGRATION_WIZARD_EXPLICITLY
        [MenuItem(OpenMenuPath, true, Priority)]
        private static bool OpenMigrationWizardValidation()
        {
            return AudioSyntaxSettings.Instance != null;
        }
#endif // ALLOW_OPENING_AUDIO_SYNTAX_MIGRATION_WIZARD_EXPLICITLY

        private void OnEnable()
        {
            Refresh();
            
            Refactor.RefactorPerformedEvent -= HandleRefactorPerformedEvent;
            Refactor.RefactorPerformedEvent += HandleRefactorPerformedEvent;
        }

        private void OnDisable()
        {
            Refactor.RefactorPerformedEvent -= HandleRefactorPerformedEvent;
        }
        
        private void HandleRefactorPerformedEvent(Refactor refactor)
        {
            Refresh();
        }

        private void Refresh()
        {
            refreshProgressId = Progress.Start(ProgressTitle, ProgressInfo);
            
            versionMigratingFrom = AudioSyntaxSettings.Instance.Version;
            versionMigratingTo = AudioSyntaxSettings.TargetVersion;

            hasDetectedIssues = false;
            detectedIssueUrgency = Migration.IssueUrgencies.Optional;

            int refreshStepCount = 2 + Migrations.Length;
            int refreshStep = 0;
            Progress.Report(refreshProgressId, refreshStep++, refreshStepCount);
            
            if (versionMigratingFrom < versionMigratingTo)
            {
                for (int i = 0; i < Migrations.Length; i++)
                {
                    Migrations[i].UpdateConditions(versionMigratingFrom);

                    if (Migrations[i].HasNecessaryRefactors)
                    {
                        hasDetectedIssues = true;
                        if (Migrations[i].Urgency > detectedIssueUrgency)
                            detectedIssueUrgency = Migrations[i].Urgency;
                    }
                    
                    Progress.Report(refreshProgressId, refreshStep++, refreshStepCount);
                }
            }
            
            Progress.Report(refreshProgressId, refreshStep++, refreshStepCount);
            
            Progress.Finish(refreshProgressId);
        }

        private void OnGUI()
        {
            bool requiresMigration = true;
            if (versionMigratingFrom > versionMigratingTo)
            {
                EditorGUILayout.HelpBox($"Hello time traveler. It was detected that you used a future version of " +
                                        $"{AudioSyntaxMenuPaths.ProjectName}. We can't help you with that yet.", MessageType.Info);
                requiresMigration = false;
            }
            else if (versionMigratingFrom == versionMigratingTo)
            {
                requiresMigration = false;
                EditorGUILayout.HelpBox($"It looks like your version of {AudioSyntaxMenuPaths.ProjectName} is up to date!", MessageType.Info);
            }
            
            bool shouldRefresh = GUILayout.Button("Refresh");
            if (shouldRefresh)
                Refresh();
            
            if (!requiresMigration)
                return;
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);

            for (int i = 0; i < Migrations.Length; i++)
            {
                Migration migration = Migrations[i];
                if (!migration.IsNecessaryForCurrentVersion)
                    continue;

                BeginSettingsBox(migration.DisplayName);
                
                migration.OnGUI();
                
                EndSettingsBox();
            }
            
            EditorGUILayout.EndScrollView();

            if (hasDetectedIssues)
            {
                if (detectedIssueUrgency == Migration.IssueUrgencies.Required)
                    EditorGUILayout.HelpBox("Issues were detected that need to be resolved first.", MessageType.Error);
                else
                    EditorGUILayout.HelpBox("Issues were detected that you should consider resolving first.", MessageType.Warning);
                if (GUILayout.Button("Fix All Automatically", GUILayout.Height(ThickButtonHeight)))
                {
                    bool confirmed = EditorUtility.DisplayDialog("Automatic Refactor All Confirmation",
                        $"You are about to let {AudioSyntaxMenuPaths.ProjectName} try to automatically refactor " +
                        $"everything in one go. Doing it step-by-step and commiting the changes to version control " +
                        $"after every step makes you more aware of what changes and gives you more control over it. " +
                        $"It's worth considering doing it that way. It's also fine to do them all at once, and " +
                        $"only *if* something goes wrong, *then* go back and do them step-by-step.\n\n" +
                        $"We HIGHLY recommend that you commit all your changes to version control first so that you " +
                        $"don't lose any work.",
                        "Yes, I have saved my work.", "No");
                    
                    if (confirmed)
                        AutoFixAll();
                }
            }
            
            using (new EditorGUI.DisabledScope(hasDetectedIssues))
            {
                bool shouldFinalize = GUILayout.Button("Finalize", GUILayout.Height(ThickButtonHeight));
                if (shouldFinalize)
                    FinalizeMigration();
            }
        }

        private void AutoFixAll()
        {
            for (int i = 0; i < Migrations.Length; i++)
            {
                Migration migration = Migrations[i];

                migration.PerformAllRefactors();
            }
        }

        private void FinalizeMigration()
        {
            using SerializedObject so = new(AudioSyntaxSettings.Instance);
            so.Update();
            so.FindProperty("version").intValue = AudioSyntaxSettings.TargetVersion;
            so.ApplyModifiedPropertiesWithoutUndo();
            
            Close();
        }
    }
}
