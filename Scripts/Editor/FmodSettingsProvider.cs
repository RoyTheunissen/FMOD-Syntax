using System.Collections.Generic;
using UnityEditor;

#if FMOD_AUTO_REGENERATE_CODE
namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Exposes serialized editor-time FMOD preferences. 
    /// </summary>
    public static class FmodPreferences
    {
        public static readonly EditorPreferenceBool GenerateCodeAutomaticallyPreference =
            new EditorPreferenceBool("FMOD/Generate Code Automatically", true);
    }
    
    /// <summary>
    /// Provides a window with which to tweak FMOD preferences such as whether to generate code automatically or not.
    /// </summary>
    public class FmodSettingsProvider : SettingsProvider
    {
        public FmodSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            
            EditorGUILayout.Space();
            FmodPreferences.GenerateCodeAutomaticallyPreference.DrawGUILayoutLeft();
        }
        
        [SettingsProvider]
        public static SettingsProvider CreateFMODSettingsProvider()
        {
            var provider = new FmodSettingsProvider("Preferences/FMOD", SettingsScope.User)
            {
                label = "FMOD",
                keywords = new HashSet<string>(new[] { "FMOD", "Audio" })
            };

            return provider;
        }
    }
}
#endif // FMOD_AUTO_REGENERATE_CODE
