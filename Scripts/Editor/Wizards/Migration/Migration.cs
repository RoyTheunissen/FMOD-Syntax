using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Base class for a migration that the Migration Wizard can perform.
    /// For example migrating from FMOD-Syntax to Audio-Syntax.
    /// </summary>
    public abstract class Migration
    {
        public enum IssueUrgencies
        {
            Required,
            Optional,
        }
        
        public abstract string DisplayName { get; }

        private int versionMigratingFrom;
        protected int VersionMigratingFrom => versionMigratingFrom;

        public abstract int VersionMigratingTo { get; }
        
        public abstract string Description { get; }
        
        public abstract string DocumentationURL { get; }

        private readonly List<Refactor> refactors = new();
        
        public delegate void IssueDetectedHandler(Migration migration, IssueUrgencies urgency);
        public event IssueDetectedHandler IssueDetectedEvent;

        private void Initialize()
        {
            RegisterRefactors(refactors);
        }

        protected abstract void RegisterRefactors(List<Refactor> refactors);

        public void UpdateConditions(int versionMigratingFrom)
        {
            this.versionMigratingFrom = versionMigratingFrom;

            for (int i = 0; i < refactors.Count; i++)
            {
                refactors[i].CheckIfNecessary();
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.HelpBox(Description, MessageType.Info);
            
            bool didClickDocumentation = EditorGUILayout.LinkButton("Documentation");
            if (didClickDocumentation)
                Application.OpenURL(DocumentationURL);
            
            EditorGUILayout.Space();

            for (int i = 0; i < refactors.Count; i++)
            {
                refactors[i].OnGUI();
                
                if (i < refactors.Count - 1)
                    EditorGUILayout.Space();
            }
        }

        protected void ReportIssue(IssueUrgencies urgency)
        {
            IssueDetectedEvent?.Invoke(this, urgency);
        }

        public static T Create<T>() where T : Migration, new()
        {
            T migration = new();
            migration.Initialize();
            return migration;
        }
    }
}
