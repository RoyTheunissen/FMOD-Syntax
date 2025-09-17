using UnityEditor;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Window to show to users when they haven't set up the FMOD Syntax system yet to let them conveniently
    /// initialize it with appropriate settings.
    /// </summary>
    public partial class MigrationWizard
    {
        private void DrawMigrationFromFmodSyntaxToAudioSyntax()
        {
            BeginSettingsBox("FMOD-Syntax to Audio-Syntax");
            EditorGUILayout.HelpBox("The system has since been updated to support Unity-based audio as well, and has been renamed from FMOD-Syntax to Audio-Syntax. Certain Namespaces / classes have been renamed, let's make sure those are now updated.", MessageType.Warning);
            EndSettingsBox();
        }
    }
}
