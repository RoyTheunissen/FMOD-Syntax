using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Inspector reference to FMOD audio config. Can be used to get a simple parameterless audio config.
    /// </summary>
    [Serializable]
    public class AudioReference : IAudioConfig
    {
        [SerializeField] private string fmodEventGuid;
        
        [NonSerialized] private FmodParameterlessAudioConfig cachedFmodAudioConfig;
        [NonSerialized] private string guidFmodAudioConfigIsCachedFor;
        private FmodParameterlessAudioConfig FmodAudioConfig
        {
            get
            {
                if (guidFmodAudioConfigIsCachedFor != fmodEventGuid)
                {
                    guidFmodAudioConfigIsCachedFor = fmodEventGuid;
                    cachedFmodAudioConfig = GetParameterlessEventConfig(fmodEventGuid);
                }
                return cachedFmodAudioConfig;
            }
        }

        public bool IsAssigned => FmodAudioConfig != null;

        public string Name => IsAssigned ? FmodAudioConfig.Name : "";
        public string Path => IsAssigned ? FmodAudioConfig.Path : "";

        [NonSerialized] private static bool didCacheParameterlessEvents;
        private static readonly Dictionary<string, FmodParameterlessAudioConfig> parameterlessEventsByGuid =
            new Dictionary<string, FmodParameterlessAudioConfig>();

        public FmodParameterlessAudioPlayback Play(Transform source = null)
        {
            return FmodAudioConfig.Play(source);
        }
        
        IAudioPlayback IAudioConfig.Play(Transform source)
        {
            return Play(source);
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            parameterlessEventsByGuid.Clear();
            didCacheParameterlessEvents = false;
        }
#endif // UNITY_EDITOR
        
        
        public static FmodParameterlessAudioConfig GetParameterlessEventConfig(string guid)
        {
            CacheParameterlessEventConfigs();
            
            if (parameterlessEventsByGuid.TryGetValue(guid, out FmodParameterlessAudioConfig result))
                return result;

            return null;
        }
        
        private static void CacheParameterlessEventConfigs()
        {
            if (didCacheParameterlessEvents)
                return;

            didCacheParameterlessEvents = true;
            
            parameterlessEventsByGuid.Clear();
            
            const string containerClassName = "AudioParameterlessEvents";
            IEnumerable<Type> containerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes()).Where(t => t.Name == containerClassName);

            foreach (Type containerType in containerTypes)
            {
                // Get the dictionary of parameter events by GUID.
                FieldInfo eventsByGuidDictionaryField = containerType.GetField(
                    "EventsByGuid", BindingFlags.Public | BindingFlags.Static);
                Dictionary<string, FmodParameterlessAudioConfig> eventsByGuidDictionary = 
                    (Dictionary<string, FmodParameterlessAudioConfig>)eventsByGuidDictionaryField.GetValue(null);
                
                // Compile them all into one big dictionary.
                foreach (KeyValuePair<string,FmodParameterlessAudioConfig> kvp in eventsByGuidDictionary)
                {
                    parameterlessEventsByGuid.Add(kvp.Key, kvp.Value);
                }
            }
        }

        public override string ToString()
        {
            FmodParameterlessAudioConfig config = FmodAudioConfig;
            return config == null ? "" : config.ToString();
        }
    }
}
