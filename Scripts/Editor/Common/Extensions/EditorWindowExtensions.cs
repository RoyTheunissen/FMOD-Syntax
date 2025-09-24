using UnityEditor;

namespace RoyTheunissen.AudioSyntax
{
    public static class EditorWindowExtensions
    {
        public static string DrawFolderPathField(
            this EditorWindow editorWindow, string currentPath, string label, string tooltip = null)
        {
            bool didPick = DrawingUtilities.DrawFolderPathField(ref currentPath, label, tooltip);

            if (didPick)
                EditorApplication.delayCall += editorWindow.Repaint;

            return currentPath;
        }
    }
}
