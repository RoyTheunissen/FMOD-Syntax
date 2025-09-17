using System;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public abstract class WizardBase : EditorWindow
    {
        private static readonly Color WarningColor = Color.Lerp(Color.yellow, Color.red, 0.0f);
        private static readonly Color SuccessColor = Color.green;
        
        [NonSerialized] private Color preValidityCheckContentColor;
        [NonSerialized] private Color preValidityCheckBackgroundColor;
        
        [NonSerialized] private Color preWarningContentColor;
        [NonSerialized] private Color preWarningBackgroundColor;
        
        [NonSerialized] private Color preSuccessContentColor;
        [NonSerialized] private Color preSuccessBackgroundColor;

        protected void BeginValidityChecks(bool isValid)
        {
            preValidityCheckContentColor = GUI.contentColor;
            preValidityCheckBackgroundColor = GUI.backgroundColor;
            
            if (isValid)
                return;
            
            GUI.contentColor = Color.Lerp(Color.white, Color.red, 0.5f);
            GUI.backgroundColor = Color.red;
        }

        protected void EndValidityChecks()
        {
            GUI.contentColor = preValidityCheckContentColor;
            GUI.backgroundColor = preValidityCheckBackgroundColor;
        }

        protected void BeginWarning()
        {
            preWarningContentColor = GUI.contentColor;
            preWarningBackgroundColor = GUI.backgroundColor;
            
            GUI.contentColor = WarningColor;
            GUI.backgroundColor = WarningColor;
        }

        protected void EndWarning()
        {
            GUI.contentColor = preWarningContentColor;
            GUI.backgroundColor = preWarningBackgroundColor;
        }

        protected void BeginSuccess()
        {
            preSuccessContentColor = GUI.contentColor;
            preSuccessBackgroundColor = GUI.backgroundColor;
            
            GUI.contentColor = SuccessColor;
            GUI.backgroundColor = SuccessColor;
        }

        protected void EndSuccess()
        {
            GUI.contentColor = preSuccessContentColor;
            GUI.backgroundColor = preSuccessBackgroundColor;
        }

        protected void BeginSettingsBox(string title)
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
        }

        protected void EndSettingsBox()
        {
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
        }
    }
}
