using UnityEngine;

namespace RoyTheunissen.FMODWrapper.Runtime
{
    /// <summary>
    /// Scriptable object that holds all the settings for the FMOD wrapper system.
    /// </summary>
    public class FmodWrapperSettings : ScriptableObject 
    {
        [SerializeField] private string generatedScriptsFolderPath;
        public string GeneratedScriptsFolderPath => generatedScriptsFolderPath;

        [SerializeField] private string namespaceForGeneratedCode;
        public string NamespaceForGeneratedCode => namespaceForGeneratedCode;

        public void InitializeFromWizard(string generatedScriptsFolderPath, string namespaceForGeneratedCode)
        {
            this.generatedScriptsFolderPath = generatedScriptsFolderPath;
            this.namespaceForGeneratedCode = namespaceForGeneratedCode;
        }
    }
}
