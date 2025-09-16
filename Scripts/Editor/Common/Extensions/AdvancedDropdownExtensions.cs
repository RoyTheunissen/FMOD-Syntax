using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    public static class AdvancedDropdownExtensions
    {
        public static void Show(this AdvancedDropdown dropdown, Rect buttonRect, float minHeight)
        {
            dropdown.Show(buttonRect);
            SetMinHeightForOpenedPopup(minHeight);
        }
 
        /// <summary>
        /// Adapted from this:
        /// https://forum.unity.com/threads/add-maximum-window-size-to-advanceddropdown-control.724229/#post-5814880
        ///
        /// Could maybe do with a cleanup...
        /// </summary>
        private static void SetMinHeightForOpenedPopup(float minHeight)
        {
            EditorWindow window = EditorWindow.focusedWindow;
 
            if (window == null)
            {
                Debug.LogWarning("EditorWindow.focusedWindow was null.");
                return;
            }
 
            if (!string.Equals(
                    window.GetType().Namespace, typeof(AdvancedDropdown).Namespace, StringComparison.Ordinal))
            {
                Debug.LogWarning("EditorWindow.focusedWindow " + EditorWindow.focusedWindow.GetType().FullName 
                                                               + " was not in expected namespace.");
                return;
            }
            
            window.minSize = new Vector2(window.minSize.x, minHeight);
        }
    }
}
