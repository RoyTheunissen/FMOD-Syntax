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
        
        private const int Priority = 1;
        
        private const string OpenMenuPath = AudioSyntaxMenuPaths.Root + "Open Migration Wizard";
        
        private const float Width = 500;
        
        private Vector2 scrollPosition;

        private int versionMigratingFrom;
        private int versionMigratingTo;

        private bool hasDetectedIssuesThatNeedToBeResolvedFirst;

        [MenuItem(OpenMenuPath, false, Priority)]
        public static void OpenMigrationWizard()
        {
            MigrationWizard migrationWizard = GetWindow<MigrationWizard>(
                false, AudioSyntaxMenuPaths.ProjectName + " Migration Wizard");
            migrationWizard.minSize = new Vector2(Width, 150);
            migrationWizard.Initialize();
        }
        
        [MenuItem(OpenMenuPath, true, Priority)]
        private static bool OpenMigrationWizardValidation()
        {
            return AudioSyntaxSettings.Instance != null;
        }

        private void Initialize()
        {
            versionMigratingFrom = AudioSyntaxSettings.Instance.Version;
            versionMigratingTo = AudioSyntaxSettings.CurrentVersion;

            hasDetectedIssuesThatNeedToBeResolvedFirst = false;
            DetectOutdatedNamespaceUsage();
            DetectOutdatedSystemReferences();
        }

        private void OnEnable()
        {
            Initialize();
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

        private void Refresh()
        {
            Initialize();
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
