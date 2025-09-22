using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public static class DrawingUtilities
    {
        private static GUIContent helpBoxSuccessGuiContent;
        
        private static readonly EditorAssetReference<Texture2D> HelpBoxCheckmark = new("HelpBoxCheckmark");
        
        public static void HelpBoxAffirmative(string message)
        {
            if (helpBoxSuccessGuiContent == null)
                helpBoxSuccessGuiContent = new GUIContent(message, HelpBoxCheckmark.Asset);
            else
                helpBoxSuccessGuiContent.text = message;

            EditorGUILayout.LabelField(helpBoxSuccessGuiContent, EditorStyles.helpBox);
        }
    }
}
