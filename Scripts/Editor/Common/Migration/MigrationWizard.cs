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
        private const string OpenMenuPath = AudioSyntaxMenuPaths.Root + "Open Migration Wizard";
        
        private const float Width = 500;
        
        private Vector2 scrollPosition;

        private int versionMigratingFrom;
        private int versionMigratingTo;

        [MenuItem(OpenMenuPath, false)]
        public static void OpenMigrationWizard()
        {
            MigrationWizard migrationWizard = GetWindow<MigrationWizard>(
                false, AudioSyntaxMenuPaths.ProjectName + " Migration Wizard");
            migrationWizard.minSize = new Vector2(Width, 150);
            migrationWizard.Initialize();
        }
        
        [MenuItem(OpenMenuPath, true)]
        private static bool OpenMigrationWizardValidation()
        {
            return AudioSyntaxSettings.Instance != null;
        }

        private void Initialize()
        {
            versionMigratingFrom = AudioSyntaxSettings.Instance.Version;
            versionMigratingTo = AudioSyntaxSettings.CurrentVersion;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox($"It was detected that you used an earlier version of " +
                                       $"{AudioSyntaxMenuPaths.ProjectName} and that certain changes need to be made " +
                                       $"before your project is in working order again.", MessageType.Info);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);

            if (versionMigratingFrom < 1)
                DrawMigrationFromFmodSyntaxToAudioSyntax();
            
            EditorGUILayout.EndScrollView();

            bool shouldFinalize = GUILayout.Button("Finalize", GUILayout.Height(48));
            if (shouldFinalize)
                FinalizeMigration();
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
        }
    }
}
