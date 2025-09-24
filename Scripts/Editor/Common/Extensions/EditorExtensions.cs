using UnityEditor;

namespace RoyTheunissen.AudioSyntax
{
    public static class EditorExtensions
    {
        public static void DrawFolderPathFieldLayout(this Editor editor, SerializedProperty property)
        {
            bool didPick = DrawingUtilities.DrawFolderPathField(property);

            if (didPick)
                EditorApplication.delayCall += editor.Repaint;
        }
    }
}
