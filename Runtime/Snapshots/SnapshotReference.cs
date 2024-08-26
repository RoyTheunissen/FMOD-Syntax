using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Inspector reference to FMOD snapshot config. Can be used to specify via the inspector which snapshot to play.
    /// </summary>
    [Serializable]
    public class SnapshotReference
    {
        [SerializeField] private string fmodSnapshotGuid;
        
        [NonSerialized] private FmodSnapshotConfig cachedFmodSnapshotConfig;
        [NonSerialized] private string guidFmodSnapshotConfigIsCachedFor;
        private FmodSnapshotConfig FmodSnapshotConfig
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
        private static readonly Dictionary<string, FmodSnapshotConfig> snapshotsByGuid =
            new Dictionary<string, FmodSnapshotConfig>();

        public FmodSnapshotPlayback Play()
        {
            return FmodSnapshotConfig.Play();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            snapshotsByGuid.Clear();
            didCacheSnapshots = false;
        }
#endif // UNITY_EDITOR
        
        
        public static FmodSnapshotConfig GetSnapshotConfig(string guid)
        {
            CacheSnapshotConfigs();
            
            if (snapshotsByGuid.TryGetValue(guid, out FmodSnapshotConfig result))
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
                    if (fields[i].FieldType != typeof(FmodSnapshotConfig))
                        continue;

                    // If the field is a valid snapshot config, add it to the dictionary.
                    if (fields[i].GetValue(null) is FmodSnapshotConfig snapshotConfig)
                        snapshotsByGuid.Add(snapshotConfig.Id, snapshotConfig);
                }
            }
        }

        public override string ToString()
        {
            FmodSnapshotConfig config = FmodSnapshotConfig;
            return config == null ? "" : config.ToString();
        }
    }
}
