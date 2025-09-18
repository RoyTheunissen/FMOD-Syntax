using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Window to show to users when they haven't set up the FMOD Syntax system yet to let them conveniently
    /// initialize it with appropriate settings.
    /// </summary>
    public partial class MigrationWizard : WizardBase
    {
        public static readonly string MigrationNecessaryText = $"It was detected that you used an earlier version of " +
                                                     $"{AudioSyntaxMenuPaths.ProjectName} and that certain changes " +
                                                     $"need to be made before your project is in working order again.";

        private static readonly string ProgressTitle = $"Detecting {AudioSyntaxMenuPaths.ProjectName} migration status";
        private const string ProgressInfo = "In the process of checking what version you're on, and if you're behind, what changes are necessary for you to be up-to-date.";
        
        private const int Priority = 1;
        
        private const string OpenMenuPath = AudioSyntaxMenuPaths.Root + "Open Migration Wizard";
        
        private const float Width = 500;
        
        private Vector2 scrollPosition;

        private int versionMigratingFrom;
        private int versionMigratingTo;

        private bool hasDetectedIssuesThatNeedToBeResolvedFirst;
        
        private int refreshProgressId;

        [MenuItem(OpenMenuPath, false, Priority)]
        public static void OpenMigrationWizard()
        {
            MigrationWizard migrationWizard = GetWindow<MigrationWizard>(
                false, AudioSyntaxMenuPaths.ProjectName + " Migration Wizard");
            migrationWizard.minSize = new Vector2(Width, 150);
            migrationWizard.Refresh();
        }
        
        [MenuItem(OpenMenuPath, true, Priority)]
        private static bool OpenMigrationWizardValidation()
        {
            return AudioSyntaxSettings.Instance != null;
        }

        private void OnEnable()
        {
            Refresh();
        }
        
        private void Refresh()
        {
            const int refreshSteps = 3;
            refreshProgressId = Progress.Start(ProgressTitle, ProgressInfo);
            
            versionMigratingFrom = AudioSyntaxSettings.Instance.Version;
            versionMigratingTo = AudioSyntaxSettings.CurrentVersion;

            hasDetectedIssuesThatNeedToBeResolvedFirst = false;

            Progress.Report(refreshProgressId, 1, refreshSteps);
            
            if (versionMigratingFrom < versionMigratingTo)
            {
                DetectOutdatedNamespaceUsage();
                
                Progress.Report(refreshProgressId, 2, refreshSteps);
                
                DetectOutdatedSystemReferences();
            }
            
            Progress.Report(refreshProgressId, 3, refreshSteps);
            
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
            else
            {
                EditorGUILayout.HelpBox(MigrationNecessaryText, MessageType.Info);
            }
            
            bool shouldRefresh = GUILayout.Button("Refresh");
            if (shouldRefresh)
                Refresh();
            
            if (!requiresMigration)
                return;
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);

            DrawMigrationFromFmodSyntaxToAudioSyntax();
            
            EditorGUILayout.EndScrollView();
            
            using (new EditorGUI.DisabledScope(hasDetectedIssuesThatNeedToBeResolvedFirst))
            {
                bool shouldFinalize = GUILayout.Button("Finalize", GUILayout.Height(48));
                if (shouldFinalize)
                    FinalizeMigration();
            }
        }

        private void ReportIssueThatNeedsToBeResolvedFirst()
        {
            hasDetectedIssuesThatNeedToBeResolvedFirst = true;
        }

        private void FinalizeMigration()
        {
            // Update the version that's saved in the settings config.
            using (SerializedObject so = new(AudioSyntaxSettings.Instance))
            {
                so.Update();
                SerializedProperty versionProperty = so.FindProperty("version");
                versionProperty.intValue = versionMigratingTo;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            
            Refresh();
        }
    }
}
