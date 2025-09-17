using System;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Window to show to users when they haven't set up the FMOD Syntax system yet to let them conveniently
    /// initialize it with appropriate settings.
    /// </summary>
    public class MigrationWizard : EditorWindow
    {
        private const string OpenMenuPath = AudioSyntaxMenuPaths.Root + "Open Migration Wizard";
        
        private const float Width = 500;
        
        private Vector2 scrollPosition;
        
        [MenuItem(OpenMenuPath, false)]
        public static void OpenMigrationWizard()
        {
            MigrationWizard setupWizard = GetWindow<MigrationWizard>(
                true, AudioSyntaxMenuPaths.ProjectName + " Migration Wizard");
            setupWizard.minSize = setupWizard.maxSize = new Vector2(Width, 550);
        }
        
        [MenuItem(OpenMenuPath, true)]
        private static bool OpenMigrationWizardValidation()
        {
            return AudioSyntaxSettings.Instance != null;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox($"It was detected that you used an earlier version of " +
                                       $"{AudioSyntaxMenuPaths.ProjectName} and that certain changes need to be made " +
                                       $"before your project is in working order again.", MessageType.Info);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, true);
            
            EditorGUILayout.EndScrollView();
        }
    }
}
