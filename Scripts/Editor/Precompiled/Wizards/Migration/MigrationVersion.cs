using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Responsible for managing the version of the system, which is stored in a AudioSyntaxSettings scriptable object
    /// config but we do not want that type to be pre-compiled because then it would be a lot harder to modify / it
    /// would increase the complexity of the package a bunch. So we have to jump through some hoops to get/set the
    /// version from that config because we cannot reference its type.
    /// </summary>
    public static class MigrationVersion
    {
        public const int TargetVersion = 1;
        
        private const string VersionField = "version";

        public static int CurrentVersion
        {
            get
            {
                using SerializedObject so = new(SettingsAsset);
                so.Update();
                return so.FindProperty(VersionField).intValue;
            }
            set
            {
                using SerializedObject so = new(SettingsAsset);
                so.Update();
                so.FindProperty(VersionField).intValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        
        [NonSerialized] private static ScriptableObject cachedSettingsAsset;
        [NonSerialized] private static bool didCacheSettingsAsset;
        private static ScriptableObject SettingsAsset
        {
            get
            {
                if (!didCacheSettingsAsset)
                {
                    cachedSettingsAsset = TryToFindAudioSyntaxSystemSettingsAsset();
                    didCacheSettingsAsset = cachedSettingsAsset != null;
                }
                return cachedSettingsAsset;
            }
        }

        private static ScriptableObject TryToFindAudioSyntaxSystemSettingsAsset()
        {
            string[] guids = AssetDatabase.FindAssets($"t:AudioSyntaxSettings");
            string guid = guids.FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);

            return AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        }
    }
}
