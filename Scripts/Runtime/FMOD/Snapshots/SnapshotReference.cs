using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Inspector reference to FMOD snapshot config. Can be used to specify via the inspector which snapshot to play.
    /// </summary>
    [Serializable]
    public class SnapshotReference
    {
        [SerializeField] private string fmodSnapshotGuid;
        
        [NonSerialized] private FmodSnapshotConfigBase cachedFmodSnapshotConfig;
        [NonSerialized] private string guidFmodSnapshotConfigIsCachedFor;
        private FmodSnapshotConfigBase FmodSnapshotConfig
        {
            get
            {
                if (guidFmodSnapshotConfigIsCachedFor != fmodSnapshotGuid)
                {
                    guidFmodSnapshotConfigIsCachedFor = fmodSnapshotGuid;
                    cachedFmodSnapshotConfig = GetSnapshotConfig(fmodSnapshotGuid);
                    if (!string.IsNullOrEmpty(fmodSnapshotGuid) && cachedFmodSnapshotConfig == null)
                    {
                        Debug.LogError($"FMOD event was assigned to snapshot reference but its corresponding config " +
                                       $"could not be found at runtime. Did you forget to compile FMOD code?");
                    }
                }
                return cachedFmodSnapshotConfig;
            }
        }

        public bool IsAssigned => FmodSnapshotConfig != null;

        public string Name => IsAssigned ? FmodSnapshotConfig.Name : "";
        public string Path => IsAssigned ? FmodSnapshotConfig.Path : "";

        [NonSerialized] private static bool didCacheSnapshots;
        private static readonly Dictionary<string, FmodSnapshotConfigBase> snapshotsByGuid =
            new Dictionary<string, FmodSnapshotConfigBase>();

        public FmodSnapshotPlayback Play()
        {
            return FmodSnapshotConfig.PlayGeneric();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            snapshotsByGuid.Clear();
            didCacheSnapshots = false;
        }
#endif // UNITY_EDITOR
        
        
        public static FmodSnapshotConfigBase GetSnapshotConfig(string guid)
        {
            CacheSnapshotConfigs();
            
            if (snapshotsByGuid.TryGetValue(guid, out FmodSnapshotConfigBase result))
                return result;

            return null;
        }
        
        private static void CacheSnapshotConfigs()
        {
            if (didCacheSnapshots)
                return;

            didCacheSnapshots = true;
            
            snapshotsByGuid.Clear();
            
            // Find every class with name AudioSnapshots.
            const string containerClassName = "AudioSnapshots";
            IEnumerable<Type> containerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes()).Where(t => t.Name == containerClassName);

            foreach (Type containerType in containerTypes)
            {
                // Find all the fields declared in those container classes.
                FieldInfo[] fields = containerType.GetFields(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!typeof(FmodSnapshotConfigBase).IsAssignableFrom(fields[i].FieldType))
                        continue;

                    // If the field is a valid snapshot config, add it to the dictionary.
                    if (fields[i].GetValue(null) is FmodSnapshotConfigBase snapshotConfig)
                        snapshotsByGuid.Add(snapshotConfig.Id, snapshotConfig);
                }
            }
        }

        public override string ToString()
        {
            FmodSnapshotConfigBase config = FmodSnapshotConfig;
            return config == null ? "" : config.ToString();
        }
    }
}
