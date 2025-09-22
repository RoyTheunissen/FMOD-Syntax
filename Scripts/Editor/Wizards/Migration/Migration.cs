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

        public abstract int VersionMigratingTo { get; }
        
        public abstract string Description { get; }
        
        public abstract string DocumentationURL { get; }

        private bool isNecessaryForCurrentVersion;
        public bool IsNecessaryForCurrentVersion => isNecessaryForCurrentVersion;

        private bool hasNecessaryRefactors;
        public bool HasNecessaryRefactors => hasNecessaryRefactors;

        private IssueUrgencies urgency;
        public IssueUrgencies Urgency => urgency;

        private readonly List<Refactor> refactors = new();

        private void Initialize()
        {
            RegisterRefactors(refactors);
        }

        protected abstract void RegisterRefactors(List<Refactor> refactors);

        public void UpdateConditions(int versionMigratingFrom)
        {
            isNecessaryForCurrentVersion = versionMigratingFrom < VersionMigratingTo;

            hasNecessaryRefactors = false;
            urgency = IssueUrgencies.Optional;
            
            if (!isNecessaryForCurrentVersion)
                return;

            for (int i = 0; i < refactors.Count; i++)
            {
                refactors[i].CheckIfNecessary();

                if (refactors[i].IsNecessary)
                {
                    hasNecessaryRefactors = true;
                    if (refactors[i].Urgency > urgency)
                        urgency = refactors[i].Urgency;
                }
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
        
        public void PerformAllRefactors()
        {
            if (!isNecessaryForCurrentVersion || !hasNecessaryRefactors)
                return;
            
            for (int i = 0; i < refactors.Count; i++)
            {
                if (refactors[i].IsNecessary)
                    refactors[i].PerformAsPartOfBatch();
            }
        }

        public static T Create<T>() where T : Migration, new()
        {
            T migration = new();
            migration.Initialize();
            return migration;
        }
    }
}
