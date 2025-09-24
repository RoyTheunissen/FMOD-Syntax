using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public static class DrawingUtilities
    {
        private static GUIContent helpBoxSuccessGuiContent;
        
        private static readonly EditorAssetReference<Texture2D> HelpBoxCheckmark = new("HelpBoxCheckmark");
        
        [NonSerialized] private static GUIContent cachedFolderIcon;
        [NonSerialized] private static bool didCacheFolderIcon;
        private static GUIContent FolderIcon
        {
            get
            {
                if (!didCacheFolderIcon)
                {
                    didCacheFolderIcon = true;
                    string path = "Folder Icon";
                    if (EditorGUIUtility.isProSkin)
                        path = "d_" + path;
                    
                    cachedFolderIcon = EditorGUIUtility.IconContent(path, string.Empty);
                }
                return cachedFolderIcon;
            }
        }
        
        public static void HelpBoxAffirmative(string message)
        {
            if (helpBoxSuccessGuiContent == null)
                helpBoxSuccessGuiContent = new GUIContent(message, HelpBoxCheckmark.Asset);
            else
                helpBoxSuccessGuiContent.text = message;

            EditorGUILayout.LabelField(helpBoxSuccessGuiContent, EditorStyles.helpBox);
        }


        private static string DrawFolderPathPickerInternal(string path, string label, out bool didPick)
        {
            didPick = false;
            
            float height = EditorGUIUtility.singleLineHeight;
            bool shouldPick = GUILayout.Button(FolderIcon, GUILayout.Width(1.5f * height), GUILayout.Height(height));
            if (shouldPick)
            {
                string startingPath = path.GetAbsolutePath();
                if (!Directory.Exists(startingPath))
                    startingPath = Application.dataPath;
                
                string selectedFolder = EditorUtility.OpenFolderPanel(
                    $"Pick {label} Folder", startingPath, string.Empty);
                if (!string.IsNullOrEmpty(selectedFolder) && selectedFolder.StartsWith(Application.dataPath))
                {
                    selectedFolder = selectedFolder.Substring(Application.dataPath.Length);
                    
                    if (selectedFolder.StartsWith(Path.DirectorySeparatorChar) ||
                        selectedFolder.StartsWith(Path.AltDirectorySeparatorChar))
                    {
                        selectedFolder = selectedFolder.Substring(1);
                    }

                    path = selectedFolder;

                    didPick = true;
                }
            }

            return path;
        }

        public static bool DrawFolderPathField(ref string currentPath, string label, string tooltip = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(EditorGUIUtility.labelWidth));
            currentPath = EditorGUILayout.TextField(currentPath);
            
            currentPath = DrawFolderPathPickerInternal(currentPath, label, out bool didPick);

            EditorGUILayout.EndHorizontal();
            
            return didPick;
        }

        public static bool DrawFolderPathField(SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property);

            property.stringValue = DrawFolderPathPickerInternal(
                property.stringValue, property.displayName, out bool didPick);
            
            EditorGUILayout.EndHorizontal();
            
            return didPick;
        }
    }
}
